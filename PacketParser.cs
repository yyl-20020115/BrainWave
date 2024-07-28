
namespace BrainWave;

/*  红色的是不变的
    AA 同步
    AA 同步
    20 是十进制的32，即有32个字节的payload，除掉20本身+两个AA同步+最后校验和
    
    02 代表信号值Signal
    C8 信号的值
    83 代表EEG Power开始了
    18 是十进制的24，说明EEG Power是由24个字节组成的，以下每三个字节为一组
    
    18 Delta 1/3
    D4 Delta 2/3
    8B Delta 3/3
    13 Theta 1/3
    D1 Theta 2/3
    69 Theta 3/3
    02 LowAlpha 1/3
    58 LowAlpha 2/3
    C1 LowAlpha 3/3
    17 HighAlpha 1/3
    3B HighAlpha 2/3
    DC HighAlpha 3/3
    02 LowBeta 1/3
    50 LowBeta 2/3
    00 LowBeta 3/3
    03 HighBeta 1/3
    CB HighBeta 2/3
    9D HighBeta 3/3
    03 LowGamma 1/3
    6D LowGamma 2/3
    3B LowGamma 3/3
    03 MiddleGamma 1/3
    7E MiddleGamma 2/3
    89 MiddleGamma 3/3
    
    04 代表专注度Attention
    00 Attention的值(0到100之间)
    05 代表放松度Meditation
    00 Meditation的值(0到100之间)
    D5 校验和
 */
public enum ParseResult : uint
{
    InProgress = 0,
    Complete = 1,
    CheckSumError = 2
}
public class PacketParser
{
    public const int PARSER_CODE_POOR_SIGNAL = 0x02;
    public const int PARSER_CODE_HEARTRATE = 0x03;
    public const int PARSER_CODE_ATTENTION = 0x04;
    public const int PARSER_CODE_MEDIATION = 0x05;
    public const int PARSER_CODE_BLINK_STRENGTH = 0x16;
    public const int PARSER_CODE_RAW = 0x80;
    public const int PARSER_CODE_DEBUG_ONE = 0x84;
    public const int PARSER_CODE_DEBUG_TWO = 0x85;
    public const int PARSER_CODE_EEG_POWER = 0x83;

    public const int PST_PACKET_CHECKSUM_FAILED = 0x02;
    public const int PST_NOT_YET_COMPLETE_PACKET = 0x00;
    public const int PST_PACKET_PARSED_SUCCESS = 0x01;

    public const int MESSAGE_READ_RAW_DATA_PACKET = 0x11;
    public const int MESSAGE_READ_DIGEST_DATA_PACKET = 0x12;

    private const int RAW_DATA_BYTE_LENGTH = 0x02;
    private const int EEG_DEBUG_ONE_BYTE_LENGTH = 0x05;
    private const int EEG_DEBUG_TWO_BYTE_LENGTH = 0x03;
    private const int PARSER_SYNC_BYTE = 0xAA;
    private const int PARSER_EXCODE_BYTE = 0x55;
    private const int MULTI_BYTE_CODE_THRESHOLD = 0x7F;

    private enum ParserState : uint
    {
        None = 0x00,
        Sync = 0x01,
        Check = 0x02,
        Payload_Length = 0x03,
        Payload = 0x04,
        Checksum = 0x05,
    }

    private ParserState parserState = 0;
    private int payloadLength = 0;
    private int payloadBytesReceived = 0;
    private int payloadSum = 0;
    private int checksum = 0;

    private readonly byte[] payload = new byte[0x100];

    public int PoorSignal { get; protected set; } = 0;
    public int Delta { get; protected set; } = 0;
    public int Theta { get; protected set; } = 0;
    public int LowAlpha { get; protected set; } = 0;
    public int HighAlpha { get; protected set; } = 0;
    public int LowBeta { get; protected set; } = 0;
    public int HighBeta { get; protected set; } = 0;
    public int LowGamma { get; protected set; } = 0;
    public int MiddleGamma { get; protected set; } = 0;

    public int BlinkStrength { get; protected set; } = 0;
    public int Attention { get; protected set; } = 0;
    public int HeartRate { get; protected set; } = 0;
    public int Mediation { get; protected set; } = 0;
    public int RawWaveData { get; protected set; } = 0;
    public bool IsStatPacket { get; protected set; } = false;
    public bool IsEarphoneError { get; protected set; } = false;


    public PacketParser()
    {
        this.parserState = ParserState.Sync;
    }


    public ParseResult ParseByte(byte current)
    {
        //SMALL PACKET:AA AA 04 80 02 xxHigh xxLow xxCheckSum
        //BIG PACKET: AA AA 20 02 C8 83 18 
        ParseResult result = ParseResult.InProgress;
        switch (this.parserState)
        {
            case ParserState.Sync: //AA
                if ((current & 0xFF) != PARSER_SYNC_BYTE)
                    break;
                this.parserState = ParserState.Check;
                break;
            case ParserState.Check: //AA
                this.parserState = (current & 0xFF) == PARSER_SYNC_BYTE
                    ? ParserState.Payload_Length
                    : ParserState.Sync
                    ;
                break;
            case ParserState.Payload_Length: //Length
                this.payloadLength = (current & 0xFF);
                this.payloadBytesReceived = 0;
                this.payloadSum = 0;
                this.parserState = ParserState.Payload;
                break;
            case ParserState.Payload:
                this.payload[(this.payloadBytesReceived++)] = current;
                this.payloadSum += (current & 0xFF);
                if (this.payloadBytesReceived < this.payloadLength)
                    break;
                this.parserState = ParserState.Checksum;
                break;
            case ParserState.Checksum:
                this.checksum = (current & 0xFF);
                this.parserState = ParserState.Sync;
                if (this.checksum != ((this.payloadSum ^ 0xFFFFFFFF) & 0xFF))
                {
                    result = ParseResult.CheckSumError;
                }
                else //last step
                {
                    result = ParseResult.Complete;
                    ParsePacketPayload();
                }
                break;
        }
        return result;
    }
    private void ParsePacketPayload()
    {
        int i = 0;
        int extendedCodeLevel = 0;
        int code = 0;
        int valueBytesLength = 0;
        IsStatPacket = this.payloadLength > 4;
        while (i < this.payloadLength)
        {
            extendedCodeLevel++;
            while (this.payload[i] == PARSER_EXCODE_BYTE) i++;
            code = this.payload[(i++)] & 0xFF;
            if (code > MULTI_BYTE_CODE_THRESHOLD)
            {
                valueBytesLength = this.payload[(i++)] & 0xFF;
            }
            else
            {
                valueBytesLength = 1;
            }
            if (code == PARSER_CODE_RAW)
            {
                if ((valueBytesLength == RAW_DATA_BYTE_LENGTH))
                {
                    RawWaveData = GetRawWaveValue(i);
                    //if (RawWaveData > 32768) RawWaveData -= 65536;

                    //Console.WriteLine("Raw:" + rawWaveData);
                }
                else
                {

                }
                i += valueBytesLength;
            }
            else
            {
                switch (code)
                {
                    case PARSER_CODE_POOR_SIGNAL:
                        PoorSignal = this.payload[i] & 0xFF;
                        i += valueBytesLength;
                        break;
                    case PARSER_CODE_EEG_POWER:
                        //EEGLength = this.payload[i] & 0xFF;
                        if (valueBytesLength >= 24 && i + valueBytesLength <= this.payload.Length)
                        {
                            Delta = GetParameterValue(i + 0);
                            Theta = GetParameterValue(i + 3);
                            LowAlpha = GetParameterValue(i + 6);
                            HighAlpha = GetParameterValue(i + 9);
                            LowBeta = GetParameterValue(i + 12);
                            HighBeta = GetParameterValue(i + 15);
                            LowGamma = GetParameterValue(i + 18);
                            MiddleGamma = GetParameterValue(i + 21);
                        }
                        i += valueBytesLength;
                        break;
                    case PARSER_CODE_ATTENTION:
                        //Signal 等于以下值，代表耳机没有戴好
                        if (PoorSignal is 29 or 54 or 55 or 56 or 80 or 81 or 82 or 107 or 200)
                        {
                            Attention = this.payload[i] & 0xFF;
                            IsEarphoneError = true;
                            i += valueBytesLength;
                            break;
                        }
                        else
                        {
                            Attention = this.payload[i] & 0xFF;
                        }
                        i += valueBytesLength;
                        break;
                    case PARSER_CODE_HEARTRATE:
                        HeartRate = this.payload[i] & 0xFF;
                        i += valueBytesLength;
                        break;
                    case PARSER_CODE_DEBUG_ONE:
                        if (valueBytesLength == EEG_DEBUG_ONE_BYTE_LENGTH)
                        {
                            i += valueBytesLength;
                        }
                        break;
                    case PARSER_CODE_DEBUG_TWO:
                        if (valueBytesLength == EEG_DEBUG_TWO_BYTE_LENGTH)
                        {
                            i += valueBytesLength;
                        }
                        break;
                    case PARSER_CODE_MEDIATION:
                        Mediation = this.payload[i] & 0xFF;
                        i += valueBytesLength;
                        break;
                    case PARSER_CODE_BLINK_STRENGTH:
                        BlinkStrength = this.payload[i] & 0xFF;
                        i+= valueBytesLength; 
                        break;
                }
            }
        }
        this.parserState = ParserState.Sync;
    }

    private int GetParameterValue(int i)
        => this.payload[i] << 16 | (this.payload[i + 1] << 8) | (this.payload[i + 2]);

    private short GetRawWaveValue(int i)
        => (short)((short)((this.payload[i + 0]) << 8) | (short)this.payload[i + 1]);
}
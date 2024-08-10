using System.IO;

namespace AnswerCalls.STT
{
    internal interface ISpeech_To_Text
    {
        void Transcribe_File(Stream stream);
    }
}

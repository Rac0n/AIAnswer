using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using System.Globalization;

namespace AnswerCalls.STT
{
    internal class Speech_STT : ISpeech_To_Text
    {
        public void Transcribe_File(Stream stream)
        {
            using (SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine(new CultureInfo("it-IT")))
            {
                recognizer.LoadGrammar(new DictationGrammar());

                recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);

                recognizer.SetInputToWaveStream(stream);

                recognizer.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        static void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Console.WriteLine("Recognized text: " + e.Result.Text);
        }
    }
}

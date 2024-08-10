using AnswerCalls.STT;
using NAudio.Wave;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Windows;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AnswerCalls
{
    class Program
    {
        private const string SIP_SERVER = "172.20.252.26";
        private const int SIP_PORT = 5060;
        private const string SIP_USERNAME = "6666";
        private const string SIP_PASSWORD = "callc";
        private const int DEFAULT_EXPIRY = 120;

        private static ISpeech_To_Text stt;

        private static SIPTransport _sipTransport;
        private static SIPRegistrationUserAgent _userAgent;
        private static SIPUserAgent _ua;

        private static readonly WaveFormat _waveFormat = new WaveFormat(8000, 16, 1);
        private static WaveFileWriter _waveFile;
        private static string outputFilePath;

        private static bool _onlyOne = true;

        static async Task Main()
        {
            var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "NAudio");
            Directory.CreateDirectory(outputFolder);
            outputFilePath = Path.Combine(outputFolder, "output.wav");
            _waveFile = new WaveFileWriter(outputFilePath, _waveFormat);

            stt = new Speech_STT();

            _sipTransport = new SIPTransport();
            _sipTransport.AddSIPChannel(new SIPUDPChannel(new IPEndPoint(IPAddress.Any, SIP_PORT)));

            _userAgent = new(_sipTransport, SIP_USERNAME, SIP_PASSWORD, $"{SIP_SERVER}:{SIP_PORT}", DEFAULT_EXPIRY);

            _userAgent.RegistrationFailed += (uri, resp, err) =>
            {
                Console.WriteLine($"{uri}: {err}");
            };
            _userAgent.RegistrationTemporaryFailure += (uri, resp, err) =>
            {
                Console.WriteLine($"{uri}: {err}");
            };
            _userAgent.RegistrationRemoved += (uri, resp) =>
            {
                Console.WriteLine($"{uri} registration failed.");
            };
            _userAgent.RegistrationSuccessful += (uri, resp) =>
            {
                Console.WriteLine($"{uri} registration succeeded.");

                Console.WriteLine(">>>>>");
                Console.WriteLine(resp.ToString());
                Console.WriteLine(">>>>>");

                _ua = new SIPUserAgent(_sipTransport, null, true);
                _ua.ServerCallCancelled += (uas, req) => Console.WriteLine("Incoming call cancelled by remote party.");
                _ua.OnCallHungup += (dialogue) => Console.WriteLine("Hanging up." +dialogue.ToString());
                _ua.OnIncomingCall += async (ua, req) =>
                {
                    Console.WriteLine(">>>>>");
                    Console.WriteLine(req.ToString());
                    Console.WriteLine(">>>>>");

                    var uas = ua.AcceptCall(req);

                    WindowsAudioEndPoint winAudioEP = new WindowsAudioEndPoint(new AudioEncoder());
                    //winAudioEP.RestrictFormats((format) => format.Codec == AudioCodecsEnum.G729);

                    VoIPMediaSession voipMediaSession = new VoIPMediaSession(winAudioEP.ToMediaEndPoints());

                    
                    voipMediaSession.AcceptRtpFromAny = true;
                    voipMediaSession.OnRtpPacketReceived += OnRtpPacketReceived;

                    await Task.Delay(500);

                    await ua.Answer(uas, voipMediaSession);

                    if (ua.IsCallActive)
                    {
                        await voipMediaSession.Start();
                        await winAudioEP.PauseAudioSink();
                    }
                };
            };

            _userAgent.Start();

            Console.ReadLine();
            _ua.Hangup();
            _waveFile?.Close();

            _userAgent.Stop();
            Task.Delay(1500).Wait();
            _sipTransport.Shutdown();
        }

        private static void OnRtpPacketReceived(IPEndPoint remoteEndPoint, SDPMediaTypesEnum mediaType, RTPPacket rtpPacket)
        {
            if (mediaType == SDPMediaTypesEnum.audio)
            {
                var sample = rtpPacket.Payload;

                /*if (rtpPacket.Header.PayloadType == (int)SDPWellKnownMediaFormatsEnum.G729)
                {
                    G729Decoder decoder = new G729Decoder();

                    byte[] pcmSample = decoder.Process(sample);

                    Console.WriteLine(pcmSample.Length);

                    _waveFile.Write(pcmSample, 0, pcmSample.Length);
                }
                else {*/
                if (_onlyOne)
                {
                    Console.WriteLine(">><< TYPE " + rtpPacket.Header.PayloadType);
                }
                    for (int index = 0; index < sample.Length; index++)
                    {
                        if (rtpPacket.Header.PayloadType == (int)SDPWellKnownMediaFormatsEnum.PCMA)
                        {
                            if (_onlyOne)
                            {
                                Console.WriteLine(">><< TYPE "+rtpPacket.Header.PayloadType);
                                _onlyOne = false;
                                Console.WriteLine("ALAW");
                            }
                            short pcm = NAudio.Codecs.ALawDecoder.ALawToLinearSample(sample[index]);
                            byte[] pcmSample = new byte[] { (byte)(pcm & 0xFF), (byte)(pcm >> 8) };
                            _waveFile.Write(pcmSample, 0, 2);

                            stt.Transcribe_File(_waveFile);
                        }
                        else
                        {
                            if (_onlyOne)
                            {
                                Console.WriteLine(">><< TYPE " + rtpPacket.Header.PayloadType);
                                _onlyOne = false;
                                Console.WriteLine("MULAW");
                            }
                            short pcm = NAudio.Codecs.MuLawDecoder.MuLawToLinearSample(sample[index]);
                            byte[] pcmSample = new byte[] { (byte)(pcm & 0xFF), (byte)(pcm >> 8) };
                            _waveFile.Write(pcmSample, 0, 2);
                        }
                    }
                //}
            }
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageSharp;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ProjectOxford.Face.Contract;
using MimeKit;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Steeltoe.CloudFoundry.Connector.Rabbit;
using Steeltoe.Extensions.Configuration;

namespace AvatarMaker.EmailSender
{
    public class Program
    {
        const string EmailAddress = "";
        const string SmtpServer = "smtp.gmail.com";
        const string SmtpServerLogin = "";
        const string SmtpServerPassword = "";

        public static void Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddCloudFoundry()
                .Build();
            services.AddRabbitConnection(config);
            var connectionFactory = services.BuildServiceProvider().GetService<ConnectionFactory>();

#if DEBUG
            // for running on local machine
            connectionFactory.HostName = "localhost";
#endif

            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                CreateQueue(channel);
                while (true)
                {
                    var data = channel.BasicGet("q.send", true);
                    if (data != null)
                    {
                        var request = JsonConvert.DeserializeObject<CropAndSendRequest>(Encoding.UTF8.GetString(data.Body));
                        DownloadImage(request.Url).ContinueWith(m =>
                        {
                            m.Result.Seek(0, SeekOrigin.Begin);  
                                                      
                            var croppedImages = CropImage(m.Result, request.Rectangles);
                            SendImages(croppedImages, request.Email, request.Url);
                        });

                    }
                    Thread.Sleep(300);
                }
            }
        }

        private static void SendImages(IList<Stream> croppedImages, string email, string url)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Test", EmailAddress));
            message.To.Add(new MailboxAddress("Test", email));
            message.Subject = "Your AvatarMaker Results for " + url;
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = croppedImages.Any() ? @"See attached files" : "No faces detected"
            };
            for (int i = 0; i < croppedImages.Count; i++)
            {
                bodyBuilder.Attachments.Add(i + 1 + ".jpg", croppedImages[i]);
            }
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                client.Connect(SmtpServer, 587, false);
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Authenticate(SmtpServerLogin, SmtpServerPassword);
                client.Send(message);
                client.Disconnect(true);
            }
        }

        private static List<Stream> CropImage(Stream image, FaceRectangle[] rectangles)
        {
            var image1 = new Image(image);
            List<Stream> result = new List<Stream>();
            foreach (var rectangle in rectangles)
            {
                var s = new MemoryStream();
                var zoom = 0.7;
                int h = (int) (rectangle.Height/zoom);
                int w = (int) (rectangle.Width/zoom);
                Image<Color, uint> aa = image1.Crop(w, h,
                    new Rectangle((int) (rectangle.Left - w * (1/zoom - 1)/2), (int)(rectangle.Top - h * (1 / zoom - 1) / 2), w, h));
                aa.SaveAsJpeg(s);
                result.Add(s);
            }
            return result;
        }

        private static async Task<Stream> DownloadImage(string url)
        {
            var stream = new MemoryStream();
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    using (Stream contentStream = await (await httpClient.SendAsync(request)).Content.ReadAsStreamAsync())
                    {
                        await contentStream.CopyToAsync(stream);
                    }
                }
            }
            return stream;
        }

        private static void CreateQueue(IModel channel)
        {
            channel.QueueDeclare(queue: "q.send",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        }
    }
}

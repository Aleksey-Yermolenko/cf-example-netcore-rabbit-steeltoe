using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Steeltoe.CloudFoundry.Connector.Rabbit;
using Steeltoe.Extensions.Configuration;

namespace AvatarMaker.Recognizer
{
    public class Program
    {
        private static readonly IFaceServiceClient FaceServiceClient = new FaceServiceClient("token");

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
                    var data = channel.BasicGet("q.recognize", true);
                    if (data != null)
                    {
                        var request = JsonConvert.DeserializeObject<RecognizeRequest>(Encoding.UTF8.GetString(data.Body));
                        UploadAndDetectFaces(request.Url).ContinueWith(m =>
                        {
                            if (m.IsCompleted)
                            {
                                var body = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(new CropAndSendRequest()
                                {
                                    Email = request.Email,
                                    Url = request.Url,
                                    Rectangles = m.Result
                                }));
                                
                                channel.BasicPublish(exchange: "e.send",
                                    routingKey: "",
                                    basicProperties: null,
                                    body: body);
                            }
                        });

                    }
                    Thread.Sleep(300);
                }
            }
        }

        private static async Task<FaceRectangle[]> UploadAndDetectFaces(string url)
        {
            try
            {
                var faces = await FaceServiceClient.DetectAsync(url);
                var faceRects = faces.Select(face => face.FaceRectangle);
                return faceRects.ToArray();
            }
            catch (Exception)
            {
                return new FaceRectangle[0];
            }
        }

        private static void CreateQueue(IModel channel)
        {
            channel.ExchangeDeclare("e.send", ExchangeType.Direct); 
            channel.QueueDeclare(queue: "q.send",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
            channel.QueueBind("q.send", "e.send", "");
        }
    }
}

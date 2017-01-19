using System.Text;
using System.Web.Http;
using AvatarMaker.Web.Requests;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;

namespace AvatarMaker.Web.Controllers
{
    [Route("api/[controller]")]
    public class MainController : ApiController
    {
        private readonly ConnectionFactory _rabbitConnection;

        public MainController([FromServices] ConnectionFactory rabbitConnection)
        {
            _rabbitConnection = rabbitConnection;

#if DEBUG
            // for running on local machine
            _rabbitConnection.HostName = "localhost";
#endif
        }

        [HttpPost]
        public IActionResult Post([FromBody] RecognizeRequest request)
        {
            using (var connection = _rabbitConnection.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                CreateQueue(channel);
                var body = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(request));
                channel.BasicPublish(exchange: "e.recognize",
                    routingKey: "",
                    basicProperties: null,
                    body: body);
            }

            return Ok();
        }

        private void CreateQueue(IModel channel)
        {
            channel.ExchangeDeclare("e.recognize", ExchangeType.Direct);
            channel.QueueDeclare(queue: "q.recognize",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            channel.QueueBind("q.recognize", "e.recognize", "");
        }
    }
}

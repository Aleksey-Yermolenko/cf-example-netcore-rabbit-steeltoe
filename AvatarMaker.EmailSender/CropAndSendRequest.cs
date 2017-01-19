using Microsoft.ProjectOxford.Face.Contract;

namespace AvatarMaker.EmailSender
{
    public class CropAndSendRequest
    {
        public string Url { get; set; }
        public string Email { get; set; }
        public FaceRectangle[] Rectangles { get; set; }
    }
}

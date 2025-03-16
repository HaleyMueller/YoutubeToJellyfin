using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeToMusic.BLL
{
    public class ErrorModel
    {
        public DateTime DateTime { get; set; } = DateTime.Now;
        public string Message { get; set; }
        public Exception? Exception { get; set; }

        public ErrorModel(string message)
        {
            this.Message = message;
        }

        public ErrorModel (string message, Exception exception)
        {
            this.Message = message;
            this.Exception = exception;
        }

        public ErrorModel (Exception exception)
        {
            this.Exception = exception;
        }

        public override string ToString()
        {
            if (Exception != null)
            {
                return $"{this.DateTime.ToShortTimeString()} - {Message}";
            }
            else
            {
                return $"{this.DateTime.ToShortTimeString()} - {Message} - {this.Exception.Message} - {this.Exception.StackTrace}";
            }
        }
    }
}

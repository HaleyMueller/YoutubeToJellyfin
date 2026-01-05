using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeToMusic.DataEntities
{
    public enum StatusEnum
    {
        Warning,
        Success,
        Error
    }

    public class DataResponse<T>
    {
        public string MessageAppender { get; set; }
        public DateTime DateTime { get; set; } = DateTime.Now;
        public T Data { get; set; }
        public StatusEnum Status {
            get
            {
                if (ResponseMessages.Any(x => x.Status == StatusEnum.Error))
                    return StatusEnum.Error;
                if (ResponseMessages.Any(x => x.Status == StatusEnum.Warning))
                    return StatusEnum.Warning;

                return StatusEnum.Success;
            }
        }

        public bool HasError
        {
            get
            {
                if (ResponseMessages.Any(x => x.Status == StatusEnum.Error))
                    return true;

                return false;
            }
        }

        public List<ResponseMessage> ResponseMessages { get; set; } = new List<ResponseMessage>();

        public DataResponse()
        {
            
        }

        public DataResponse(string appendedMessage)
        {
            this.MessageAppender = appendedMessage;
        }

        public class ResponseMessage
        {
            public DateTime DateTime { get; set; } = DateTime.Now;
            public StatusEnum Status { get; set; }
            public string Message { get; set; }
            public Exception? Exception { get; set; }

            public string MessageAppend { get; set; } //better way to do this??>????

            public override string ToString()
            {
                return $"[{Status}]{MessageAppend} {this.DateTime.ToShortTimeString()} - {this.Message}";
            }
        }

        public DataResponse<T> AddException(Exception exception)
        {
            var message = new ResponseMessage();
            message.Exception = exception;
            message.Status = StatusEnum.Error;
            message.Message = exception.Message;
            message.MessageAppend = this.MessageAppender;

            ResponseMessages.Add(message);

            return this;
        }

        public DataResponse<T> AddError(string error)
        {
            var message = new ResponseMessage();
            message.Status = StatusEnum.Error;
            message.Message = error;
            message.MessageAppend = this.MessageAppender;
            ResponseMessages.Add(message);

            return this;
        }

        public static List<ResponseMessage> CopyMessages(DataResponse<T> dataResponse)
        {
            var ret = new List<ResponseMessage>();

            foreach (var messages in dataResponse.ResponseMessages)
            {
                ret.Add(new ResponseMessage() { Message = messages.Message, Exception = messages.Exception, DateTime = messages.DateTime, Status = messages.Status, MessageAppend = messages.MessageAppend });
            }

            return ret;
        }

        public DataResponse<T> AddWarning(string warning)
        {
            var message = new ResponseMessage();
            message.Status = StatusEnum.Warning;
            message.Message = warning;
            message.MessageAppend = this.MessageAppender;
            ResponseMessages.Add(message);

            return this;
        }

        public DataResponse<T> AddMessage(string msg, StatusEnum statusEnum)
        {
            var message = new ResponseMessage();
            message.Status = statusEnum;
            message.Message = msg;
            message.MessageAppend = this.MessageAppender;
            ResponseMessages.Add(message);

            return this;
        }

        public DataResponse<T> AddMessage(string msg, StatusEnum statusEnum, Exception? ex)
        {
            var message = new ResponseMessage();
            message.Status = statusEnum;
            message.Message = msg;
            message.Exception = ex;
            message.MessageAppend = this.MessageAppender;

            if (ex != null)
                message.Status = StatusEnum.Error;

            ResponseMessages.Add(message);

            return this;
        }

        public DataResponse<T> AddException(string msg, Exception ex)
        {
            var message = new ResponseMessage();
            message.Status = StatusEnum.Error;
            message.Message = msg;
            message.MessageAppend = this.MessageAppender;
            ResponseMessages.Add(message);

            return this;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var message in ResponseMessages)
            {
                if (message.Exception == null)
                {
                    builder.AppendLine($"[{Status}]{this.MessageAppender} {this.DateTime.ToShortTimeString()} - {message.Message}");
                }
                else
                {
                    builder.AppendLine($"[{Status}]{this.MessageAppender} {this.DateTime.ToShortTimeString()} - {message.Message} - {message.Exception.Message} - {message.Exception.StackTrace}");
                }
            }

            return builder.ToString();
        }
    }
}

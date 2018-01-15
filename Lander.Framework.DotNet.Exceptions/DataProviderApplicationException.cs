using System;

namespace Lander.Framework.DotNet.Exceptions
{
    public class DataProviderApplicationException : ApplicationException
    {
        public DataProviderApplicationException(string pMessage)
          : base(pMessage)
        {
        }

        public DataProviderApplicationException(string pMessage, Exception pInnerException)
          : base(pMessage, pInnerException)
        {
        }
    }
}

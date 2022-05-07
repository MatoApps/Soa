﻿using System.Net;

namespace Soa.Protocols.Service
{
    /// <summary>
    ///     server address
    /// </summary>
    public abstract class SoaAddress
    {
        protected SoaAddress(string serverFlag)
        {
            ServerFlag = serverFlag;
        }
        //public string Token { get; set; }

        /// <summary>
        ///     whether is health of the server
        /// </summary>
        public bool IsHealth { get; set; } = true;

        public string Ip { get; set; }

        public int Port { get; set; }

        /// <summary>
        ///     the code identify the server
        /// </summary>
        public string Code => ToString();

        /// <summary>
        ///     flag of the server type
        /// </summary>
        public string ServerFlag { get; set; }

        public virtual EndPoint CreateEndPoint()
        {
            return new IPEndPoint(IPAddress.Parse(Ip), Port);
        }

        public override string ToString()
        {
            return $"{Ip}:{Port}";
        }


        #region Equality members

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            var model = obj as SoaAddress;
            if (model == null)
                return false;

            if (obj.GetType() != GetType())
                return false;

            return model.ToString() == ToString();
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }


        public static bool operator ==(SoaAddress model1, SoaAddress model2)
        {
            return Equals(model1, model2);
        }

        public static bool operator !=(SoaAddress model1, SoaAddress model2)
        {
            return !Equals(model1, model2);
        }

        #endregion Equality members
    }
}
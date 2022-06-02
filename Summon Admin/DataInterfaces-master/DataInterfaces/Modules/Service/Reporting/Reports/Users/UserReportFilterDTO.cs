using System;
using System.Runtime.Serialization;

namespace ServerService.Reporting.Reports.Users
{
    /// <summary>
    /// User Report Filter.
    /// </summary>
    [Serializable]
    [DataContract]
    public class UserReportFilterDTO : UserReportFilterBaseDTO
    {
        /// <summary>
        /// Filter User Name.
        /// </summary>
        public string UserName { get; set; }

    }
}
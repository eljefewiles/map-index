using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MapIndex.Models.Authentication;
using MapIndex.Models.ViewModels;
using Dapper;
using System.Text;

namespace MapIndex.Data.Authentication
{
    public class AuthRepository : BaseRepository<Models.Authentication.AuthModel>, IAuthRepository<Models.Authentication.AuthModel>
    {
      

        public bool IsUserValid(Models.Authentication.ViewModels.AuthViewModel authModel)
        {
            bool isValid = false;

            using (var con = GetConnection(""))
            {
                con.Open();
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT ").AppendLine();
                sb.Append(" id ").Append(" AS ").Append(" id ").Append(", ").AppendLine();

                sb.Append(" FROM ").Append(" ").AppendLine();
                sb.Append(" WHERE ").AppendLine();
                sb.Append(" UserName ").Append(" = ").Append("@userName").AppendLine();
                sb.Append(" AND ").AppendLine();
                sb.Append(" QbToken ").Append(" = ").Append("@qbToken").AppendLine();
                sb.Append(" AND ").AppendLine();
                sb.Append(" QbTicket ").Append(" = ").Append("@qbTicket").AppendLine();

                var auth = con.Query<int>(sb.ToString(), new
                {
                    userName = authModel.UserNameIncoming,
                    qbToken = authModel.QbTokenIncoming,
                    qbTicket = authModel.QbTicketIncoming
                }).FirstOrDefault();


                if (authModel != null)
                {
                    isValid = true;
                }
               
            }

            return isValid;
        }

        internal override List<IMapIndexData> QueryData(List<AuthModel> data, MapSearch map, string query)
        {
            throw new NotImplementedException();
        }
    }
}
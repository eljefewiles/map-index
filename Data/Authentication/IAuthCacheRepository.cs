using MapIndex.Infrastructure.Caching;
using MapIndex.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MapIndex.Data.Authentication
{
    public class IAuthCacheRepository : IAuthRepository<Models.Authentication.AuthModel>
    {
        private ICacheProvider _cacheProvider;
        private readonly IAuthRepository<Models.Authentication.AuthModel> _commonRepository;

        public IAuthCacheRepository(IAuthRepository<Models.Authentication.AuthModel> commonRepository, ICacheProvider cacheProvider)
        {
            _commonRepository = commonRepository;
            _cacheProvider = cacheProvider;
        }


        public bool IsUserValid(Models.Authentication.ViewModels.AuthViewModel authModel)
        {
            StringBuilder cacheKey = new StringBuilder();
            cacheKey.Append("CACHE_KEY_IS_USER_VALID").Append("_").Append("USER_ID").Append("_").Append(authModel.UserNameIncoming).Append("_").Append("QBTOKEN").Append("_").Append(authModel.QbTokenIncoming).Append("_").Append("QBTICKET").Append(authModel.QbTicketIncoming);

            //var items = _cacheProvider.Retrieve<bool>(cacheKey.ToString());

            //if (string.IsNullOrEmpty(items.ToString()))
            //{
            //    items = _commonRepository.IsUserValid(authModel);

            //    if (!string.IsNullOrEmpty(items.ToString()))
            //    {
            //        _cacheProvider.Store(cacheKey.ToString(), items, int.MaxValue);
            //    }
            //}
            bool items = true;
            return items;
        }


    }
}
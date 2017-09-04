using MapIndex.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MapIndex.Data.Authentication
{
    public interface IAuthRepository<T>
    {
        bool IsUserValid(Models.Authentication.ViewModels.AuthViewModel authModel);
    }
}
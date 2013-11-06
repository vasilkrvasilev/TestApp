using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.ValueProviders;
using System.Text;
using Test.Services.Models;
using Test.Data;
using Test.Services.Attributes;

namespace Test.Services.Controllers
{
    public class UsersController : BaseApiController
    {
        private const int MinLength = 6;
        private const int MaxPasswordLength = 30;
        private const int MaxUsernameLength = 50;
        private const string ValidUsernameCharacters =
            "qwertyuioplkjhgfdsazxcvbnmQWERTYUIOPLKJHGFDSAZXCVBNM1234567890_.";
        private const string ValidNameCharacters =
            "qwertyuioplkjhgfdsazxcvbnmQWERTYUIOPLKJHGFDSAZXCVBNM";

        //private const string SessionKeyChars =
        //    "qwertyuioplkjhgfdsazxcvbnmQWERTYUIOPLKJHGFDSAZXCVBNM";
        //private static readonly Random rand = new Random();

        //private const int SessionKeyLength = 50;

        //private const int Sha1Length = 40;

        public UsersController()
        {
        }

        /*
        {  "username": "DonchoMinkov",
           "nickname": "Doncho Minkov",
           "authCode":   "bfff2dd4f1b310eb0dbf593bd83f94dd8d34077e" }

{
"username": "ssssss",
"displayName": "SSSSSS",
"authCode": "bfff2dd4f1d310eb0dbf593bd83f94dd8d34077s"
}
        */

        [ActionName("register")]
        public HttpResponseMessage PostRegisterUser(UserModel model)
        {
            var responseMsg = this.PerformOperationAndHandleExceptions(
                () =>
                {
                    var context = new dbf609f467420e40209014a26e008b568aEntities();
                    using (context)
                    {
                        this.ValidateUsername(model.Username);
                        this.ValidateName(model.Name);
                        this.ValidatePassword(model.Password);
                        var usernameToLower = model.Username.ToLower();
                        var nameToLower = model.Name.ToLower();
                        var user = context.Users.FirstOrDefault(
                            usr => usr.Username == usernameToLower
                            || usr.Name.ToLower() == nameToLower);

                        if (user != null)
                        {
                            throw new InvalidOperationException("User already exists");
                        }

                        user = new User()
                        {
                            Username = usernameToLower,
                            Name = model.Name,
                            Password = model.Password
                        };

                        context.Users.Add(user);
                        context.SaveChanges();

                        user.SessionKey = user.Id.ToString();
                        context.SaveChanges();

                        var loggedModel = new UserLoggedModel()
                        {
                            Name = user.Name,
                            SessionKey = user.SessionKey
                        };

                        var response =
                            this.Request.CreateResponse(HttpStatusCode.Created,
                                            loggedModel);
                        return response;
                    }
                });

            return responseMsg;
        }

        [ActionName("login")]
        public HttpResponseMessage PostLoginUser(UserModel model)
        {
            var responseMsg = this.PerformOperationAndHandleExceptions(
              () =>
              {
                  var context = new dbf609f467420e40209014a26e008b568aEntities();
                  using (context)
                  {
                      this.ValidateUsername(model.Username);
                      this.ValidatePassword(model.Password);
                      var usernameToLower = model.Username.ToLower();
                      var user = context.Users.FirstOrDefault(
                          usr => usr.Username == usernameToLower
                          && usr.Password == model.Password);

                      if (user == null)
                      {
                          throw new InvalidOperationException("Invalid username or password");
                      }

                      if (user.SessionKey == null)
                      {
                          user.SessionKey = user.Id.ToString();
                          context.SaveChanges();
                      }

                      var loggedModel = new UserLoggedModel()
                      {
                          Name = user.Name,
                          SessionKey = user.SessionKey
                      };

                      var response =
                          this.Request.CreateResponse(HttpStatusCode.Created,
                                          loggedModel);
                      return response;
                  }
              });

            return responseMsg;
        }

        [ActionName("logout")]
        public HttpResponseMessage PutLogoutUser(
            [ValueProvider(typeof(HeaderValueProviderFactory<string>))] string sessionKey)
        {
            var responseMsg = this.PerformOperationAndHandleExceptions(
              () =>
              {
                  var context = new dbf609f467420e40209014a26e008b568aEntities();
                  using (context)
                  {
                      var user = context.Users.FirstOrDefault(
                          usr => usr.SessionKey == sessionKey);

                      if (user == null)
                      {
                          throw new InvalidOperationException("Invalid sessionKey");
                      }

                      user.SessionKey = null;
                      context.SaveChanges();

                      var response =
                          this.Request.CreateResponse(HttpStatusCode.OK);
                      return response;
                  }
              });

            return responseMsg;
        }

        [ActionName("info")]
        public UserModel GetUserInfo(
            [ValueProvider(typeof(HeaderValueProviderFactory<string>))] string sessionKey)
        {
            var responseMsg = this.PerformOperationAndHandleExceptions(() =>
            {
                var context = new dbf609f467420e40209014a26e008b568aEntities();

                var user = context.Users.FirstOrDefault(
                    usr => usr.SessionKey == sessionKey);
                if (user == null)
                {
                    throw new InvalidOperationException("Invalid sessionKey");
                }

                var model = new UserModel()
                {
                    Name = user.Name,
                    Username = user.Username,
                    Password = user.Password
                };

                return model;
            });

            return responseMsg;
        }

        //private string GenerateSessionKey(int userId)
        //{
        //    StringBuilder skeyBuilder = new StringBuilder(SessionKeyLength);
        //    skeyBuilder.Append(userId);
        //    while (skeyBuilder.Length < SessionKeyLength)
        //    {
        //        var index = rand.Next(SessionKeyChars.Length);
        //        skeyBuilder.Append(SessionKeyChars[index]);
        //    }
        //    return skeyBuilder.ToString();
        //}

        private void ValidatePassword(string password)
        {
            if (password == null || password.Length < MinLength || password.Length > MaxPasswordLength)
            {
                throw new ArgumentOutOfRangeException("Password is invalid");
            }
        }

        private void ValidateName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("Nickname cannot be null");
            }
            else if (name.Length < MinLength)
            {
                throw new ArgumentOutOfRangeException(
                    string.Format("Name must be at least {0} characters long",
                    MinLength));
            }
            else if (name.Length > MaxUsernameLength)
            {
                throw new ArgumentOutOfRangeException(
                    string.Format("Name must be less than {0} characters long",
                    MaxUsernameLength));
            }
            else if (name.Any(ch => !ValidNameCharacters.Contains(ch)))
            {
                throw new ArgumentOutOfRangeException(
                    "Name must contain only Latin letters");
            }
        }

        private void ValidateUsername(string username)
        {
            if (username == null)
            {
                throw new ArgumentNullException("Username cannot be null");
            }
            else if (username.Length < MinLength)
            {
                throw new ArgumentOutOfRangeException(
                    string.Format("Username must be at least {0} characters long",
                    MinLength));
            }
            else if (username.Length > MaxUsernameLength)
            {
                throw new ArgumentOutOfRangeException(
                    string.Format("Username must be less than {0} characters long",
                    MaxUsernameLength));
            }
            else if (username.Any(ch => !ValidUsernameCharacters.Contains(ch)))
            {
                throw new ArgumentOutOfRangeException(
                    "Username must contain only Latin letters, digits .,_");
            }
        }
    }
}

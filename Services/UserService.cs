using System;
using System.Collections.Generic;
using System.Linq;
using WebApi.Entities;
using WebApi.Helpers;
using Novell.Directory.Ldap;

namespace WebApi.Services
{
    public interface IUserService
    {
        Auth Authenticate(string username, string password);
        IEnumerable<Auth> GetAll();
        Auth GetById(int id);
        Auth Create(Auth user, string password);
        void Update(Auth user, string password = null);
        void Delete(int id);
    }

    public class UserService : IUserService
    {
        private DataContext _context;

        public UserService(DataContext context)
        {
            _context = context;
        }

        public Auth Authenticate(string username, string password)
        {
            string objectDN = "cn=" + username + ",ou=pinart,dc=pinart,dc=com";

            LdapConnection conn = _context.connectLDAP();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            var user = _context.Auths.SingleOrDefault(x => x.Username == username);

            // check if username exists
            if (user == null)
                return null;

            // check if password is correct
            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;

            // ldap Authentication
            if (_context.auth(conn, objectDN, password))
            {
                Console.WriteLine("Successful Connection LDAP");
                // authentication successful
                conn.Disconnect();
                return user;
            }

            conn.Disconnect();
            return null;
        }

        public IEnumerable<Auth> GetAll()
        {
            return _context.Auths;
        }

        public Auth GetById(int id)
        {
            return _context.Auths.Find(id);
        }

        public Auth Create(Auth user, string password)
        {
            string container = "ou=pinart,dc=pinart,dc=com";
            LdapConnection conn = _context.connectLDAP();

            // validation
            if (string.IsNullOrWhiteSpace(password))
                throw new AppException("Password is required");

            if (_context.Auths.Any(x => x.Username == user.Username))
                throw new AppException("Username \"" + user.Username + "\" is already taken");


            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            _context.Auths.Add(user);
            _context.SaveChanges();

            _context.createLDAP(conn, container, user.FirstName, user.LastName, user.Username, user.Username, password);
            conn.Disconnect();

            return user;
        }

        public void Update(Auth userParam, string password = null)
        {
            var user = _context.Auths.Find(userParam.Id);

            if (user == null)
                throw new AppException("User not found");

            // update username if it has changed
            if (!string.IsNullOrWhiteSpace(userParam.Username) && userParam.Username != user.Username)
            {
                // throw error if the new username is already taken
                if (_context.Auths.Any(x => x.Username == userParam.Username))
                    throw new AppException("Username " + userParam.Username + " is already taken");

                user.Username = userParam.Username;
            }

            // update user properties if provided
            if (!string.IsNullOrWhiteSpace(userParam.FirstName))
                user.FirstName = userParam.FirstName;

            if (!string.IsNullOrWhiteSpace(userParam.LastName))
                user.LastName = userParam.LastName;

            // update password if provided
            if (!string.IsNullOrWhiteSpace(password))
            {
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(password, out passwordHash, out passwordSalt);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
            }

            _context.Auths.Update(user);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var user = _context.Auths.Find(id);
            if (user != null)
            {
                _context.Auths.Remove(user);
                _context.SaveChanges();
            }
        }

        // private helper methods

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }

            return true;
        }
    }
}
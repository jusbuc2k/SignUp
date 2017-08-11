using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Registration.Models.Data;
using Microsoft.Extensions.Options;
using Dapper;
using Registration.Models;

namespace Registration.Services
{

    public class DbAccessOptions
    {
        public string ConnectionString { get; set; }
    }

    public class DbAccess : IDataAccess, IDisposable
    {
        private DbAccessOptions _options;
        private System.Data.Common.DbConnection _connection;

        public DbAccess(IOptions<DbAccessOptions> options)
        {
            _options = options.Value;
        }

        protected async Task<System.Data.Common.DbConnection> EnsureConnection()
        {
            if (_connection == null)
            {
                _connection = new System.Data.SqlClient.SqlConnection(_options.ConnectionString);
            }

            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            return _connection;
        }

        public async Task<LoginToken> CreateLoginToken(string emailAddress, string personID)
        {
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var buffer = new byte[6];

            rng.GetBytes(buffer);
            
            var code = new string(buffer.Select(b => (char)((int)Math.Floor((b / 255D) * 10D) + 48)).ToArray());

            var tokenRecord = new LoginToken()
            {
                TokenID = Guid.NewGuid(),
                Token = code,
                BadAttemptCount = 0,
                EmailAddress = emailAddress,
                PersonID = personID,
                ExpiresDateTime = DateTimeOffset.Now.AddMinutes(30)
            };

            var connection = await this.EnsureConnection();

            await connection.ExecuteAsync(@"
                INSERT INTO dbo.LoginToken(TokenID, Token, BadAttemptCount, EmailAddress, PersonID, ExpiresDateTime) 
                VALUES (@TokenID, @Token, @BadAttemptCount, @EmailAddress, @PersonID, @ExpiresDateTime)
            ", tokenRecord);

            return tokenRecord;                       
        }

        public async Task<VerifyLoginTokenResult> VerifyLoginToken(Guid tokenID, string token)
        {
            var connection = await this.EnsureConnection();

            var matches = await this._connection.QueryAsync<LoginToken>(@"
                SELECT * FROM dbo.LoginToken WHERE TokenID = @TokenID AND Token = @Token;
            ", new
            {
                TokenID = tokenID,
                Token = token
            });

            if (matches.Any(x => x.ExpiresDateTime > DateTimeOffset.Now && x.BadAttemptCount < 4))
            {
                var match = matches.First();

                await connection.ExecuteAsync(@"
                    DELETE FROM dbo.LoginToken WHERE TokenID = @TokenID;
                ", new
                {
                    tokenID
                });

                return new VerifyLoginTokenResult()
                {
                    Success = true,
                    BadAttemptCount = 0,
                    EmailAddress = match.EmailAddress,
                    ExpiresDateTime = match.ExpiresDateTime,
                    PersonID = match.PersonID,
                    Token = match.Token,
                    TokenID = match.TokenID
                };
            }
            else
            {
                await connection.ExecuteAsync(@"
                    UPDATE dbo.LoginToken SET BadAttemptCount = BadAttemptCount + 1 WHERE TokenID = @TokenID;
                ", new
                {
                    tokenID
                });

                return new VerifyLoginTokenResult()
                {
                    Success = false
                };
            }
        }

        public async Task<IEnumerable<EventModel>> GetEventList()
        {
            var connection = await this.EnsureConnection();

            return await connection.QueryAsync<EventModel>(@"
                SELECT * FROM dbo.Event 
                WHERE BeginDate < @Now AND EndDate > @Now 
                ORDER BY [Name]
            ", new
            {
                Now = DateTimeOffset.Now
            });
        }
        
        public async Task<EventModel> GetEvent(Guid eventID)
        {
            var connection = await this.EnsureConnection();

            var evt = await connection.QueryFirstAsync<EventModel>(@"
                SELECT * FROM dbo.Event 
                WHERE EventID = @EventID
            ", new
            {
                EventID = eventID
            });

            evt.Fees = await connection.QueryAsync<EventFee>(@"
                SELECT * FROM dbo.EventFee 
                WHERE EventID = @EventID
            ", new
            {
                EventID = eventID
            });

            return evt;
        }

        public async Task CreateEventPerson(EventPerson person)
        {
            var connection = await this.EnsureConnection();

            await connection.ExecuteAsync(@"
                INSERT INTO dbo.EventPerson(CreateDateTime,EventID,PersonID,HouseholdID,HouseholdName,FirstName,LastName,Child,BirthDate,Grade,Gender,EmailAddress,PhoneNumber,MedicalNotes,Group)
                VALUES(SYSDATETIMEOFFSET(),@EventID,@PersonID,@HouseholdID,@HouseholdName,@FirstName,@LastNae,@Child,@BirthDate,@Grade,@Gender,@EmailAddress,@PhoneNumber,@MedicalNotes,@Group)
            ", person);
        }


        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
            }
        }
    }
}

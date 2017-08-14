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
    public class SqlDbAccessOptions
    {
        public string ConnectionString { get; set; }
    }

    public class SqlDataAccess : IDataAccess, IDisposable
    {
        private SqlDbAccessOptions _options;
        private System.Data.Common.DbConnection _connection;

        public SqlDataAccess(IOptions<SqlDbAccessOptions> options)
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
            //TODO: The generation of the token doensn't seem like 
            // it should be at this layer. Maybe it should be passed in?
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();

            // Change the buffer length to change the number of digits in the token
            var buffer = new byte[6];

            rng.GetBytes(buffer);
            
            // Get a 6 digit string using characters between 48 and 58 (digits 0-9)
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
            var result = new VerifyLoginTokenResult()
            {
                Success = false
            };

            using (var transaction = connection.BeginTransaction())
            {
                // Fetch a matching login token by ID
                var matches = await this._connection.QueryAsync<LoginToken>(@"
                    SELECT * FROM dbo.LoginToken WHERE TokenID = @TokenID AND Token = @Token;
                    ", new {
                        TokenID = tokenID,
                        Token = token
                    }, transaction: transaction);

                // If there is a match, and it hasn't been attempted 3 times
                if (matches.Any(x => x.ExpiresDateTime > DateTimeOffset.Now && x.BadAttemptCount < 3))
                {
                    var match = matches.First();

                    // Delete the token so it can't be used again
                    await connection.ExecuteAsync(@"
                            DELETE FROM dbo.LoginToken WHERE TokenID = @TokenID;
                        ", new {
                                tokenID
                            }, transaction: transaction);

                    result.Success = true;
                    result.BadAttemptCount = 0;
                    result.EmailAddress = match.EmailAddress;
                    result.ExpiresDateTime = match.ExpiresDateTime;
                    result.PersonID = match.PersonID;
                    result.Token = match.Token;
                    result.TokenID = match.TokenID;
                }
                else
                {
                    // Increment the bad attempt counter
                    await connection.ExecuteAsync(@"
                            UPDATE dbo.LoginToken SET BadAttemptCount = BadAttemptCount + 1 WHERE TokenID = @TokenID;
                        ", new {
                            tokenID
                        }, transaction: transaction);

                    result.Success = false;
                }

                transaction.Commit();

                return result;
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
                INSERT INTO dbo.EventPerson(CreateDateTime,EventID,PersonID,HouseholdID,HouseholdName,FirstName,LastName,Child,BirthDate,Grade,Gender,EmailAddress,PhoneNumber,MedicalNotes,[Group])
                VALUES(SYSDATETIMEOFFSET(),@EventID,@PersonID,@HouseholdID,@HouseholdName,@FirstName,@LastName,@Child,@BirthDate,@Grade,@Gender,@EmailAddress,@PhoneNumber,@MedicalNotes,@Group)
            ", person);
        }

        public async Task<EventPerson> GetEventPerson(Guid eventID, string personID)
        {
            var connection = await this.EnsureConnection();

            return await connection.QuerySingleOrDefaultAsync<EventPerson>(@"
                SELECT * FROM dbo.EventPerson WHERE EventID = @EventID AND PersonID = @PersonID
            ", new
            {
                EventID = eventID,
                PersonID = personID
            });
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

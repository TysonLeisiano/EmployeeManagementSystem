using BaseLibrary.DTOs;
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerLibrary.Data;
using ServerLibrary.Helper;
using ServerLibrary.Repositories.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Constants = ServerLibrary.Helpers.Constants;



namespace ServerLibrary.Repositories.Implementations
{
    public class UserAccountRepository(IOptions<JwtSection> config, AppDbContext appDbCOntext) : IUserAccount
    {
        public async Task<GeneralResponse> CreateAsync(Register User)
        {
            if (User == null) return new GeneralResponse(false, "Model is emplty.");

            var checkUser = await FindUserByEmail(User.Email!);
            if (checkUser != null) return new GeneralResponse(false, "User is already registered.");

            // save user
            var applicationUser = await AddToDatabase(new ApplicationUser()
            {
                Fullname = User.FullName,
                Email = User.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(User.Password)

            });

            // check, create, and assign role
            var checkAdminRole = await appDbCOntext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.Admin));
            if (checkAdminRole is null)
            {
                var createAdminRole = await AddToDatabase(new SystemRoles() { Name = Constants.Admin });
                await AddToDatabase(new UserRoles() { RoleId = createAdminRole.Id, UserId = applicationUser.Id });
                return new GeneralResponse(true, "Account successfully created.");
            }

            var checkUserRole = await appDbCOntext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.User));
            SystemRoles response = new();
            if (checkAdminRole is null)
            {
                var createAdminRole = await AddToDatabase(new SystemRoles() { Name = Constants.Admin });
                await AddToDatabase(new UserRoles() { RoleId = createAdminRole.Id, UserId = applicationUser.Id });
            }
            else
            {
                await AddToDatabase(new UserRoles() { RoleId = checkUserRole.Id, UserId = applicationUser.Id });
            }
            return new GeneralResponse(true, "Account successfully created!");

        }
        public async Task<LoginResponse> SignInAsync(Login user)
        {
            if (user is null) 
                return new LoginResponse(false, "Model is emplty!");

            var applicationUser = await FindUserByEmail(user.Email!);
            if (applicationUser is null) 
                return new LoginResponse(false, "User not found!");

            // verify password
            if (!BCrypt.Net.BCrypt.Verify(user.Password, applicationUser.Password))
                return new LoginResponse(false, "Email or password not valid. Please try again.");

            // Assuming 'Role' has a 'Name' property and UserRole has 'RoleId'
            var getUserRole = await FindUserRole(applicationUser.Id);
            if (getUserRole is null) 
                return new LoginResponse(false, "User role not found!");

            // Fetch the role name from the System Roles table based on the RoleId
            var getRoleName = await FindRoleName(applicationUser.Id);
            if (getUserRole is null) 
                return new LoginResponse(false, "Role not found!");

            // Now generate the JWT token using the role name
            string jwtToken = GenerateToken(applicationUser, getRoleName!.Name!);
            string refreshToken = GenerateRefreshToken();

            //Save refresh token to the database
            var findUser = await appDbCOntext.RefreshTokenInfo.FirstOrDefaultAsync(_ => _.UserId == applicationUser.Id);
            if (findUser is not null)
            {
                findUser!.Token = refreshToken;
                await appDbCOntext.SaveChangesAsync();
            }
            else
            {
                await AddToDatabase(new RefreshTokenInfo() { Token = refreshToken, UserId = applicationUser.Id });
            }

            return new LoginResponse(true, "Login success!", jwtToken, refreshToken);
        }

        private string GenerateToken(ApplicationUser user, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Value.Key!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Fullname!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, role!)
            };
            var token = new JwtSecurityToken(
                issuer: config.Value.Issuer,
                audience: config.Value.Audience,
                claims: userClaims,
                expires: DateTime.Now.AddSeconds(2),
                signingCredentials: credentials
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<UserRoles> FindUserRole(int userId) =>
            await appDbCOntext.UserRoles.FirstOrDefaultAsync(_ => _.UserId == userId);

        private async Task<SystemRoles> FindRoleName(int roleId) =>
            await appDbCOntext.SystemRoles.FirstOrDefaultAsync(_ => _.Id == roleId);
        private static string GenerateRefreshToken() => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));

        private async Task<ApplicationUser> FindUserByEmail(string email) =>
            await appDbCOntext.ApplicationUsers.FirstOrDefaultAsync(_ => _.Email!.ToLower()!.Equals(email.ToLower()));

        private async Task<T> AddToDatabase<T>(T model)
        {
            var result = appDbCOntext.Add(model);
            await appDbCOntext.SaveChangesAsync();
            return (T)result.Entity;
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshToken token)
        {
            if (token is null) return new LoginResponse(false, "Model is empty!");
            var findToken = await appDbCOntext.RefreshTokenInfo.FirstOrDefaultAsync(_ => _.Token!.Equals(token.Token));
            if (findToken is null) return new LoginResponse(false, "Refresh token is required");

            //get user details
            var user = await appDbCOntext.ApplicationUsers.FirstOrDefaultAsync(_ => _.Id == findToken.UserId);
            if (user is null) return new LoginResponse(false, "refresh token could not be generated because user is not found");

            var userRole = await FindUserRole(user.Id);
            var roleName = await FindRoleName(userRole.RoleId);
            string jwtToken = GenerateToken(user, roleName.Name);
            string refreshToken = GenerateRefreshToken();

            var updaterefreshToken = await appDbCOntext.RefreshTokenInfo.FirstOrDefaultAsync(_ => _.UserId == user.Id);
            if (updaterefreshToken is null) return new LoginResponse(false, "referesh token could not be generated because user is not signed in");

            updaterefreshToken.Token = refreshToken;
            await appDbCOntext.SaveChangesAsync();
            return new LoginResponse(true, "Token refreshed successfully", jwtToken, refreshToken);

        }
    }
}

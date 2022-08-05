using Microsoft.AspNetCore.Mvc;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationSettings _appSettings;
        private readonly IConfiguration _config;
        private ApplicationDbContext _dbContext;
        private readonly IRUnit _unit;
        private readonly string _conString=null;
        public AccountController(UserManager<ApplicationUser> userManager, 
        SignInManager<ApplicationUser> signInManager,IOptions<ApplicationSettings> appSettings,
        IConfiguration config,ApplicationDbContext dbContext,IRUnit unit)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _appSettings = appSettings.Value;
            _config = config;
            _dbContext=dbContext;
            _unit=unit;
            _conString=_dbContext.Database.GetConnectionString();
        }

        public IConfigurationRoot Configuration { get; set; }
        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(ApplicationUserModel model)
        {
            #region  Register
            try
            {
                var applicationUser = new ApplicationUser() {
                    UserName = model.UserName,
                    Email = model.Email,
                    FullName = model.FullName
                };
                var result = await _userManager.CreateAsync(applicationUser, model.Password);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await this.BadRequest(ex);
            }
            #endregion
        }
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginModel model)
        {
            #region Login
            try
            {
                var user = await _userManager.FindByEmailAsync(model.UserName);
                if(user != null)
                {   
                    var result = await _userManager.CheckPasswordAsync(user,model.Password);
                    if (result)
                    {
                        var loginResult = await LoginSuccess(user);
                        return Ok(loginResult);
                    }
                    else
                    {
                        return BadRequest("The email or password you entered did not match our records. Please double-check and try again.");
                    }
                }
                else 
                {
                    return BadRequest("The email or password you entered did not match our records. Please double-check and try again.");
                }
            }
            catch (Exception ex)
            {
                return await this.BadRequest(ex);
            }
            #endregion
        }
        [HttpGet]
        private async Task<Object> LoginSuccess(ApplicationUser user)
        {
            try
            {
                var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings")["Token"]));
                var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256Signature);

                var roles = await _userManager.GetRolesAsync(user);
                var rolesString = JsonConvert.SerializeObject(roles);

                var tokeOptions = new JwtSecurityToken(
                            issuer: Request.Host.Value,
                            audience: Request.Host.Value,
                            claims: new List<Claim>(
                                new List<Claim> {
                                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                                    new Claim("userName",user.UserName),
                                    new Claim("email",user.Email),
                                    new Claim("role", rolesString),
                                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                                }
                            ),
                            expires: DateTime.Now.AddDays(1),
                            signingCredentials: signinCredentials
                );

                var tokenHandler = new JwtSecurityTokenHandler();
                return new
                {
                    // ok = true,
                    token = tokenHandler.WriteToken(tokeOptions)
                };
            }
            catch (Exception ex)
            {
                return await this.BadRequest(ex);
            }
            
        }
        [HttpGet("GetUserProfile")]
        public async Task<Object> GetUserProfile() {
            try
            {
                string userId = User.Claims.First(c => c.Type == "UserID").Value;
                var user = await _userManager.FindByIdAsync(userId);
                return new
                {
                    user.FullName,
                    user.Email,
                    user.UserName
                };
            }
            catch (Exception ex)
            {
                return await this.BadRequest(ex);
            }
        }
        [HttpPost("SaveOrEditCompany")]
        public async Task<IActionResult> SaveOrEditCompany(CompanyInfo objData)
        {
            #region  Save or Update Company
            using(var _dbContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    if(_unit.isSave(objData.ID))
                    {
                        objData.ID=_unit.GetNewID();
                        _unit.Company.Add(objData);
                    }else{
                        CompanyInfo data = _unit.Company.GetFirstOrDefault(x=>x.ID==objData.ID);
                        if(data==null) return NotFound(StaticMSG.NotFound(nameof(CompanyInfo)));
                        data.ID=objData.ID;
                        data.Name=objData.Name;
                        data.Address=objData.Address;
                        _unit.Company.Update(data);
                    }
                    
                    _unit.SaveChanges();
                    _dbContextTransaction.Commit();
                    return Ok(true);
                }
                catch (Exception ex)
                {
                    if(_dbContextTransaction!=null)
                        _dbContextTransaction.Rollback();
                    return await this.BadRequest(ex);
                }
            }
            #endregion
        }
        [HttpGet("DeleteCompany")]
        public async Task<IActionResult> DeleteCompany(string id)
        {
            #region  Delete Individual Company
            using(var _dbContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    var data = _unit.Company.GetFirstOrDefault(x=>x.ID==id);
                    if(data==null) return NotFound(StaticMSG.NotFound(nameof(CompanyInfo)));
                    _unit.Company.Remove(data);
                    _unit.SaveChanges();
                    _dbContextTransaction.Commit();
                    return Ok(true);
                }
                catch (Exception ex)
                {
                    if(_dbContextTransaction!=null)
                        _dbContextTransaction.Rollback();
                    return await this.BadRequest(ex);
                }
            }
            #endregion
        }
        [HttpGet("GetAllCompany")]
        public async Task<IActionResult> GetAllCompany()
        {
            #region  Get All Company
            try
            {
                var data=await _unit.Company.GetAllAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return await this.BadRequest(ex);
            }
            #endregion
        }
        [HttpGet("GetCompany")]
        public async Task<IActionResult> GetCompany(string id)
        {
            #region  Get Individual Company
            try
            {
                var data =_unit.Company.GetFirstOrDefault(x=>x.ID==id);
                if(data==null) return NotFound(StaticMSG.NotFound());
                return Ok(data);
            }
            catch (Exception ex)
            {
                return await this.BadRequest(ex);
            }
            #endregion
        }
        [HttpPost("SaveOrEditClient")]
        public async Task<IActionResult> SaveOrEditClient(Client objData)
        {
            #region  Save or Update Client
            using(var _dbContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    if(_unit.isSave(objData.ID))
                    {
                        objData.ID=_unit.GetNewID();
                        objData.UserName="zahidsarmon@gmail.com";
                        _unit.Client.Add(objData);
                        await this.Configure(objData.Name,0);
                    }else{
                        Client data = _unit.Client.GetFirstOrDefault(x=>x.ID==objData.ID);
                        if(data==null) return NotFound(StaticMSG.NotFound(nameof(Client)));
                        await this.Configure(data.Name,1);
                        data.ID=objData.ID;
                        data.Name=objData.Name;
                        _unit.Client.Update(data);
                        await this.Configure(objData.Name,0);
                    }
                    
                    _unit.SaveChanges();
                    _dbContextTransaction.Commit();
                    return Ok(true);
                }
                catch (Exception ex)
                {
                    if(_dbContextTransaction!=null)
                        _dbContextTransaction.Rollback();
                    return await this.BadRequest(ex);
                }
            }
            #endregion
        }
        [HttpGet("DeleteClient")]
        public async Task<IActionResult> DeleteClient(string id)
        {
            #region  Delete Individual Client
            using(var _dbContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    var data = _unit.Client.GetFirstOrDefault(x=>x.ID==id);
                    if(data==null) return NotFound(StaticMSG.NotFound(nameof(Client)));
                    _unit.Client.Remove(data);
                    _unit.SaveChanges();
                    await this.Configure(data.Name,1);
                    _dbContextTransaction.Commit();
                    return Ok(true);
                }
                catch (Exception ex)
                {
                    if(_dbContextTransaction!=null)
                        _dbContextTransaction.Rollback();
                    return await this.BadRequest(ex);
                }
            }
            #endregion
        }
        [HttpGet("GetAllClient")]
        public async Task<IActionResult> GetAllClient()
        {
            #region  Get All Client
            try
            {
                var data=await _unit.Client.GetAllAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return await this.BadRequest(ex);
            }
            #endregion
        }
        [HttpGet("GetAllConfigure")]
        public async Task<IActionResult> GetAllConfigure()
        {
            #region  Get All Configure
            using(SqlConnection con=new SqlConnection(_conString))
            {
                try{
                    List<string> list=new List<string>();
                    con.Open();
                    string sql="SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'Configure'";
                    SqlCommand sqlCmd=new SqlCommand(sql,con);
                    SqlDataReader sqlRead=sqlCmd.ExecuteReader();
                    while(sqlRead.Read())
                    {
                        list.Add(sqlRead["COLUMN_NAME"].ToString());
                    }
                    return Ok(list);
                }catch(Exception ex)
                {
                    return await this.BadRequest(ex);
                }finally{
                    con.Close();
                }
            }
            #endregion
        }
        [HttpGet("GetClient")]
        public async Task<IActionResult> GetClient(string id)
        {
            #region  Get Individual Client
            try
            {
                var data =_unit.Client.GetFirstOrDefault(x=>x.ID==id);
                if(data==null) return NotFound(StaticMSG.NotFound());
                return Ok(data);
            }
            catch (Exception ex)
            {
                return await this.BadRequest(ex);
            }
            #endregion
        }
        public async Task<dynamic> Configure(string col,int isDrop)
        {
            #region  Configure
            using(SqlConnection con=new SqlConnection(_conString))
            {
                SqlTransaction transaction = null;
                try{
                    con.Open();
                    transaction = con.BeginTransaction("Configure");
                    string sql="prcConfigure";
                    SqlCommand sqlCmd=new SqlCommand(sql,con,transaction);
                    sqlCmd.CommandType=System.Data.CommandType.StoredProcedure;
                    sqlCmd.Parameters.AddWithValue("@pColumn",col);
                    sqlCmd.Parameters.AddWithValue("@pColumnDataType","nvarchar(max)");
                    sqlCmd.Parameters.AddWithValue("@pIsDrop",isDrop);
                    sqlCmd.ExecuteNonQuery();
                    transaction.Commit();
                    return true;
                }catch(Exception ex)
                {
                    if(transaction!=null)
                        transaction.Rollback();
                    throw ex;
                }finally{
                    con.Close();
                }
            }
            #endregion
        }
        public async Task<BadRequestObjectResult> BadRequest(Exception ex)
        {
            if (ex.InnerException != null)
                return BadRequest(ex.InnerException.Message);
            return BadRequest(ex.Message);
        }
    }
    
}
using AccountService.API.Contracts;
using AutoMapper;
using Application.DTOs;

namespace AccountService.API.Mapping
{
    public class AccountApiProfile : Profile
    {
        public AccountApiProfile()
        {
            // API -> Application
            CreateMap<AccountRegisterRequest, RegisterAccountDto>();

            // Application -> API
            CreateMap<AccountDto, AccountResponse>();
            CreateMap<AccountDto, AccountStatusResponse>();
        }
    }
}

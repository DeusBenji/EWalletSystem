using Application.DTOs;
using AutoMapper;
using Domain.Models;

namespace Application.Mapping
{
    public class AccountApplicationProfile : Profile
    {
        public AccountApplicationProfile()
        {
            // Domain ↔ Application
            CreateMap<Account, AccountDto>().ReverseMap();
            CreateMap<RegisterAccountDto, Account>(); // hvis du vil
        }
    }
}
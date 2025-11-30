using AutoMapper;
using BachMitID.Domain.Model;
using BachMitID.Application.DTOs;

namespace BachMitID
{
    public class MappingProfile : Profile
    {
        public MappingProfile() 
        {
            CreateMap<Account, AccountDto>();
            CreateMap<AccountDto, Account>();

            CreateMap<MitID_Account, MitIdAccountDto>();
            CreateMap<MitIdAccountDto, MitID_Account>();

        }


    }
}

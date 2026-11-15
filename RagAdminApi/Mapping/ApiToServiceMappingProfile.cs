using AutoMapper;
using Rag.Service.Dtos;
using RagAdminApi.Models;
using RagAdminApi.Requests;

namespace RagAdminApi.Mapping
{
    public class ApiToServiceMappingProfile : Profile
    {
        public ApiToServiceMappingProfile()
        {
            CreateMap<CreateServiceAgentModel, ServiceAgentDto>();
            CreateMap<EditServiceAgentModel, ServiceAgentDto>();
            CreateMap<GetAllServiceAgentsRequest, GetAllServiceAgentsFilter>();
            CreateMap<CreateKnowledgeBaseModel, KnowledgeBaseDto>();
            CreateMap<EditKnowledgeBaseModel, KnowledgeBaseDto>();
            CreateMap<CreateKnowledgeBaseCategoryModel, KnowledgeBaseCategoryDto>();
            CreateMap<EditKnowledgeBaseCategoryModel, KnowledgeBaseCategoryDto>();
            CreateMap<GetAllKnowledgeBaseCategoriesRequest, GetAllKnowledgeBaseCategoriesFilter>();
        }
    }
}

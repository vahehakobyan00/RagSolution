using AutoMapper;
using Rag.Data.Entity;
using Rag.Service.Dtos;
using RagLib.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Mapping
{
    internal class ServiceToDataMappingProfile : Profile
    {
        public ServiceToDataMappingProfile()
        {
            CreateMap<ServiceAgentDto, ServiceAgent>().ReverseMap();
            CreateMap<ServiceAgent, ServiceAgentGridItem>()
                .ForMember(dest => dest.LlmProviderName, opt => opt.MapFrom(src => src.LlmProvider.Name)); ;
            CreateMap<KnowledgeBaseCategoryDto, KnowledgeBaseCategory>().ReverseMap();
            CreateMap<KnowledgeBaseCategory, KnowledgeBaseCategoryGridItem>();
            CreateMap<KnowledgeBaseDto, KnowledgeBase>().ReverseMap();
            CreateMap<KnowledgeBase, KnowledgeBaseGridItem>();
            CreateMap<CreateKnowledgeBaseDocumentDto, KnowledgeBaseDocument>();
            CreateMap<KnowledgeBaseDocument, KnowledgeBaseDocumentDto>()
            .ForMember(dest => dest.FileItem, opt => opt.MapFrom(src => src.FileItem))
            .ForMember(dest => dest.KnowledgeBase, opt => opt.MapFrom(src => src.KnowledgeBase))
            .ForMember(dest => dest.KnowledgeBaseCategory, opt => opt.MapFrom(src => src.KnowledgeBaseCategory));
            CreateMap<FileItem, FileItemDto>().ReverseMap();
            CreateMap<EditKnowledgeBaseDocumentDto, KnowledgeBaseDocument>();
            CreateMap<Conversation, ConversationViewDto>();
            CreateMap<Turn, TurnViewDto>();
            CreateMap<ChunkDto, ChunkGridItem>();
            CreateMap<TurnFeedback, TurnFeedbackViewDto>();
            CreateMap<ServiceAgent, ServiceAgentConfigDto>();
            CreateMap<GetAllConversationsStatisticsDetailsFilter, StatisticsFilter>();
            CreateMap<GetAllConversationsStatisticsFilter, StatisticsFilter>();
        }
    }
}

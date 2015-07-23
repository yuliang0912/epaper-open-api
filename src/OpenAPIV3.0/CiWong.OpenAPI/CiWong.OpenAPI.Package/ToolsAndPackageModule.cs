using Autofac;
using CiWong.Examination.API;
using CiWong.Framework.Cache;
using CiWong.Ques.API;
using CiWong.Tools.Package.Services;

namespace CiWong.OpenAPI.ToolsAndPackage
{
	public class ToolsAndPackageModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<PackageService>().As<PackageService>();
			//试题API
			builder.RegisterType<QuestionAPI>().As<IQuestionAPI>();
			//试卷API
			builder.RegisterType<ExaminationAPI>().As<IExaminationAPI>();
			// 试题
			builder.RegisterType<QuesService>().As<IQuesService>();
			// 知识点
			builder.RegisterType<PointsAPI>().As<IPointsAPI>();

			builder.RegisterType<MemcachedCache>().As<ICache>();
			base.Load(builder);
		}
	}
}

using Autofac;
using CiWong.Resource.Preview.Service;

namespace CiWong.OpenAPI.ExpandWork
{
	public class ResourceModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterModule<CiWong.Resource.Preview.ResourceModule>();
			base.Load(builder);
		}
	}
}

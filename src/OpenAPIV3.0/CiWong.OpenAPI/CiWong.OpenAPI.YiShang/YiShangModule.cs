using Autofac;
using CiWong.YiShang.YiShang;

namespace CiWong.OpenAPI.YiShang
{
	public class YiShangModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<ProductRepository>();
			base.Load(builder);
		}
	}
}

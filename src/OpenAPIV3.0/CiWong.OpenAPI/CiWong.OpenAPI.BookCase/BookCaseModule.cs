using Autofac;
using CiWong.Resource.BookRoom.Service;
using CiWong.Resource.BookRoom.Repository;
using CiWong.Tools.Package.Services;


namespace CiWong.OpenAPI.BookCase
{
	public class BookCaseModule : Autofac.Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<PackagePermissionRepository>().As<PackagePermissionService>();
			builder.RegisterType<ProductInfoRepository>().As<ProductInfoService>();
			builder.RegisterType<UserproductRepository>().As<UserproductService>();
			base.Load(builder);
		}
	}
}

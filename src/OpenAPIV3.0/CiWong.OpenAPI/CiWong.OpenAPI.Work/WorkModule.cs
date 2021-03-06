﻿using Autofac;
using CiWong.OpenAPI.Work.Service;
using CiWong.Work.Contract;
using CiWong.Work.Service;

namespace CiWong.OpenAPI.Work
{
	/// <summary>
	/// 作业Module
	/// </summary>
	public class WorkModule : Autofac.Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<WorkPublisher>();
			builder.RegisterType<DoWorkBaseProvider>().As<IDoWorkBase>();
			builder.RegisterType<WorkBaseProvider>().As<IWorkBase>();
			builder.RegisterType<WorkCustomProvider>().As<IWorkCustom>();
			builder.RegisterType<WorkRecordProvider>().As<IWorkRecord>();
			builder.RegisterType<BeiKeBookProvider>().As<IBeiKeBookProvider>();
			builder.RegisterType<ClassGroupProvider>().As<IClassGroupProvider>();
			builder.RegisterModule<CiWong.Resource.Preview.ResourceModule>();
			base.Load(builder);
		}
	}
}

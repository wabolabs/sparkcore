using Autofac;
using Resgrid.Model.Providers;

namespace Resgrid.Providers.PdfProvider
{
	public class PdfProviderModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<PuppeteerSharpProvider>().As<IPdfProvider>().InstancePerLifetimeScope();
			builder.RegisterType<PrintNodeProvider>().As<IPrinterProvider>().InstancePerLifetimeScope();
		}
	}
}

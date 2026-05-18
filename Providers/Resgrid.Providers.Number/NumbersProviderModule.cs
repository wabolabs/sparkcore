using Autofac;
using Resgrid.Model.Providers;

namespace Resgrid.Providers.NumberProvider
{
	public class NumbersProviderModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<NumberProviderFactory>().As<INumberProvider>().InstancePerLifetimeScope();
			builder.RegisterType<TextMessageProvider>().As<ITextMessageProvider>().InstancePerLifetimeScope();

			if (Config.SystemBehaviorConfig.OutboundVoiceProviderType == Config.OutboundVoiceProviderTypes.SipTrunk)
				builder.RegisterType<SipTrunkOutboundVoiceProvider>().As<IOutboundVoiceProvider>().InstancePerLifetimeScope();
			else
				builder.RegisterType<OutboundVoiceProvider>().As<IOutboundVoiceProvider>().InstancePerLifetimeScope();

			builder.RegisterType<PhoneNumberProcesserProvider>().As<IPhoneNumberProcesserProvider>().SingleInstance();
		}
	}
}
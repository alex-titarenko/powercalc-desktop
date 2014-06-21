﻿using Ninject.Modules;
using System;
using TAlex.Common.Environment;
using TAlex.PowerCalc.Services;
using TAlex.PowerCalc.Converters;
using TAlex.WPF.Mvvm.Services;


namespace TAlex.PowerCalc.Locators.Modules
{
    public class BaseServicesNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ApplicationInfo>().ToConstant(ApplicationInfo.Current);
            Bind<IAppSettings>().ToMethod(x => Properties.Settings.Default).InSingletonScope();
            Bind<IClipboardService>().To<ClipboardService>();
            Bind<IMessageService>().To<MessageService>();
            Bind<WorksheetMatrixCachedValueConverter>().ToSelf();
            Bind<IHowToItemsProvider>().To<HowToItemsProvider>();
        }
    }
}

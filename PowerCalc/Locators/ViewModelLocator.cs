﻿using Ninject;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TAlex.MathCore.ExpressionEvaluation.Trees.Builders;
using TAlex.MathCore.ExpressionEvaluation.Trees.Metadata;
using TAlex.PowerCalc.ViewModels;


namespace TAlex.PowerCalc.Locators
{
    public class ViewModelLocator
    {
        private IKernel _kernel;


        public ViewModelLocator()
        {
            _kernel = new StandardKernel(new MathCoreNinjectModule(), new ViewModelNinjectModule());
        }


        public AboutWindowViewModel AboutWindowViewModel
        {
            get
            {
                return _kernel.Get<AboutWindowViewModel>();
            }
        }

        public RegistrationWindowViewModel RegistrationWindowViewModel
        {
            get
            {
                return _kernel.Get<RegistrationWindowViewModel>();
            }
        }


        public InsertFunctionContextMenuViewModel InsertFunctionContextMenuViewModel
        {
            get
            {
                IList<FunctionMetadata> metadata = _kernel.Get<IFunctionsMetadataProvider>().GetMetadata().ToList();

                return new InsertFunctionContextMenuViewModel()
                {
                    Functions = metadata
                        .GroupBy(x => x.Category)
                        .Select(x => new FunctionCategoryViewModel()
                        {
                            CategoryName = x.Key,
                            Functions = x.Select(f => new FunctionViewModel
                            {
                                DisplayName = f.DisplayName,
                                FunctionName = f.Signatures.First().Name
                            }).ToList()
                        }).ToList()
                };
            }
        }

        public ConstantsContextMenuViewModel ConstantsContextMenuViewModel
        {
            get
            {
                IList<ConstantMetadata> metadata = _kernel.Get<IConstantsMetadataProvider>().GetMetadata().ToList();

                return new ConstantsContextMenuViewModel()
                {
                    Constants = metadata.Select(x => new ConstantViewModel
                    {
                        DisplayName = x.DisplayName,
                        ConstantName = x.Name,
                        Value = x.Value
                    }).ToList()
                };
            }
        }
    }

    public class MathCoreNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ConstantFlyweightFactory<Object>>()
                .ToSelf()
                .InSingletonScope()
                .OnActivation((c, i) => { i.AddFromAssemblies(GetAssembliesFromPath(Properties.Settings.Default.ExtensionsPath)); });
            Bind<IConstantFactory<Object>>().ToMethod(ctx => ctx.Kernel.Get<ConstantFlyweightFactory<Object>>());
            Bind<IConstantsMetadataProvider>().ToMethod(ctx => ctx.Kernel.Get<ConstantFlyweightFactory<Object>>());

            Bind<FunctionFactory<Object>>()
                .ToSelf()
                .InSingletonScope()
                .OnActivation((c, i) => { i.AddFromAssemblies(GetAssembliesFromPath(Properties.Settings.Default.ExtensionsPath)); });
            Bind<IFunctionFactory<Object>>().ToMethod(ctx => ctx.Kernel.Get<FunctionFactory<Object>>());
            Bind<IFunctionsMetadataProvider>().ToMethod(ctx => ctx.Kernel.Get<FunctionFactory<Object>>());

            Bind<IExpressionTreeBuilder<Object>>()
                .To<ComplexExpressionTreeBuilder>()
                .InSingletonScope()
                .OnActivation((c, i) =>
                {
                    i.ConstantFactory = c.Kernel.Get<IConstantFactory<Object>>();
                    i.FunctionFactory = c.Kernel.Get<IFunctionFactory<Object>>();
                });
        }


        private static IEnumerable<Assembly> GetAssembliesFromPath(string path)
        {
            string[] files = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path), "*.dll", SearchOption.TopDirectoryOnly);

            foreach (string filePath in files)
            {
                yield return Assembly.LoadFile(filePath);
            }
        }
    }

    public class ViewModelNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<AboutWindowViewModel>().ToSelf().InSingletonScope();
            Bind<RegistrationWindowViewModel>().ToSelf();
        }
    }
}
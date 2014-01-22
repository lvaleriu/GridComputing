using System;

namespace GridSharedLibs
{
    public static class NewAppDomain
    {
        public static void Execute(Action action)
        {
            AppDomain domain = null;

            try
            {
                domain = AppDomain.CreateDomain("New App Domain: " + Guid.NewGuid());

                var domainDelegate = (AppDomainDelegate)domain.CreateInstanceAndUnwrap(
                    typeof(AppDomainDelegate).Assembly.FullName,
                    typeof(AppDomainDelegate).FullName);

                domainDelegate.Execute(action);
            }
            finally
            {
                if (domain != null)
                    AppDomain.Unload(domain);
            }
        }

        public static void Execute<T>(T parameter, Action<T> action)
        {
            AppDomain domain = null;

            try
            {
                domain = AppDomain.CreateDomain("New App Domain: " + Guid.NewGuid());

                var domainDelegate = (AppDomainDelegate)domain.CreateInstanceAndUnwrap(
                    typeof(AppDomainDelegate).Assembly.FullName,
                    typeof(AppDomainDelegate).FullName);

                domainDelegate.Execute(parameter, action);
            }
            finally
            {
                if (domain != null)
                    AppDomain.Unload(domain);
            }
        }

        public static T Execute<T>(Func<T> action)
        {
            AppDomain domain = null;

            try
            {
                domain = AppDomain.CreateDomain("New App Domain: " + Guid.NewGuid());

                var domainDelegate = (AppDomainDelegate)domain.CreateInstanceAndUnwrap(
                    typeof(AppDomainDelegate).Assembly.FullName,
                    typeof(AppDomainDelegate).FullName);

                return domainDelegate.Execute(action);
            }
            finally
            {
                if (domain != null)
                    AppDomain.Unload(domain);
            }
        }

        public static TResult Execute<T, TResult>(T parameter, Func<T, TResult> action)
        {
            AppDomain domain = null;

            try
            {
                domain = AppDomain.CreateDomain("New App Domain: " + Guid.NewGuid());

                var domainDelegate = (AppDomainDelegate)domain.CreateInstanceAndUnwrap(
                    typeof(AppDomainDelegate).Assembly.FullName,
                    typeof(AppDomainDelegate).FullName);

                return domainDelegate.Execute(parameter, action);
            }
            finally
            {
                if (domain != null)
                    AppDomain.Unload(domain);
            }
        }

        private class AppDomainDelegate : MarshalByRefObject
        {
            public void Execute(Action action)
            {
                action();
            }

            public void Execute<T>(T parameter, Action<T> action)
            {
                action(parameter);
            }

            public T Execute<T>(Func<T> action)
            {
                return action();
            }

            public TResult Execute<T, TResult>(T parameter, Func<T, TResult> action)
            {
                return action(parameter);
            }
        }
    }

    public class SharedClass : MarshalByRefObject
    {
        public static int Counter = 1;

        public void Print()
        {
            Console.WriteLine(AppDomain.CurrentDomain.FriendlyName + " - " + Counter);
            Counter++;
        }
    }
}
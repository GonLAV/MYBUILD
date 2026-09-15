using System.Collections.Concurrent;
using System.Reflection;
using Bolt.Automation.InternalServices.Common.Interfaces;
using Castle.DynamicProxy;

namespace Bolt.Automation.InternalServices.Common
{
    internal class MicroserviceHttpInterceptor(IGenericMicroserviceClient microserviceClient) : IInterceptor
    {
        private static readonly MethodInfo _postAsyncMethodInfo = typeof(MicroserviceHttpInterceptor)
                .GetMethod(nameof(PostRequestAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly ConcurrentDictionary<Type, MethodInfo> _postAsyncRequestMethods = new();

        public void Intercept(IInvocation invocation)
        {
            var isTaskReturnType = invocation.Method.ReturnType.IsGenericType && invocation.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
            if (!isTaskReturnType)
                throw new Exception("All Microservice methods must return Task<>");

            var returnType = invocation.Method.ReturnType.GenericTypeArguments[0];

            var action = invocation.Method.Name;
            var content = invocation.Arguments[0];

            var genericMethod = _postAsyncRequestMethods.GetOrAdd(returnType,
                rt => _postAsyncMethodInfo.MakeGenericMethod(rt));

            var response = genericMethod.Invoke(this, [action, content]);
            invocation.ReturnValue = response;
        }

        private Task<T?> PostRequestAsync<T>(string action, object content) where T : class
        {
            return microserviceClient.PostAsync<T>(action, content);
        }
    }
}

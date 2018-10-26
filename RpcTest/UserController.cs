using System;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Security;

namespace RpcTest
{
    /// <summary>用户控制器。会话获取，请求过滤</summary>
    [Api("User")]
    class UserController : IApi, IActionFilter
    {
        /// <summary>会话。同一Tcp/Udp会话多次请求共用，执行服务方法前赋值</summary>
        public IApiSession Session { get; set; }

        [Api(nameof(FindByID))]
        public User FindByID(Int32 uid, Boolean deleted)
        {
            // Session 用法同Web
            var times = Session["Times"].ToInt();
            times++;
            Session["Times"] = times;

            // 故意制造异常
            if (times >= 2)
            {
                // 取得当前上下文
                var ctx = ControllerContext.Current;

                throw new ApiException(507, "[{0}]调用次数过多！Times={1}".F(ctx.ActionName, times));
            }

            var user = new User
            {
                ID = uid,
                Name = Rand.NextString(8),
                Enable = deleted,
                CreateTime = DateTime.Now,
            };

            return user;
        }

        /// <summary>本控制器执行前</summary>
        /// <param name="filterContext"></param>
        public void OnActionExecuting(ControllerContext filterContext)
        {
            // 请求参数
            var ps = filterContext.Parameters;

            // 服务参数
            var cs = filterContext.ActionParameters;

            foreach (var item in ps)
            {
                if (cs != null && !cs.ContainsKey(item.Key))
                    XTrace.WriteLine("服务[{0}]未能找到匹配参数 {1}={2}", filterContext.ActionName, item.Key, item.Value);
            }
        }

        /// <summary>本控制器执行后，包括异常发生</summary>
        /// <param name="filterContext"></param>
        public void OnActionExecuted(ControllerContext filterContext)
        {
            var ex = filterContext.Exception;
            if (ex != null && !filterContext.ExceptionHandled)
            {
                XTrace.WriteLine("控制器拦截到异常：{0}", ex.Message);
            }
        }
    }
}
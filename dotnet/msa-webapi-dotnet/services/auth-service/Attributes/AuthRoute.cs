using Microsoft.AspNetCore.Mvc.Routing;

namespace auth_service.Attributes;

/// <summary>
/// 모든 컨트롤러의 기본 경로를 "api/auth"로 설정하는 Attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AuthRoute : Attribute, IRouteTemplateProvider
{
    private readonly string _prefix = "api/auth";

    /// <summary>
    /// 컨트롤러에서 추가로 정의한 Route 값과 결합하여 최종 경로를 생성
    /// </summary>
    public string? Template { get; }

    /// <summary>
    /// 라우트 적용 우선순위 (낮을수록 우선 적용)
    /// </summary>
    public int? Order => 2;

    /// <summary>
    /// 라우트 이름 설정 가능
    /// </summary>
    public string? Name { get; set; } = default;

    /// <summary>
    /// 기본 생성자: "api/auth"만 적용
    /// </summary>
    public AuthRoute()
    {
        Template = _prefix;
    }

    /// <summary>
    /// 컨트롤러에 추가적인 하위 경로를 설정할 수 있도록 지원
    /// </summary>
    /// <param name="route">추가할 하위 경로</param>
    public AuthRoute(string route)
    {
        Template = $"{_prefix}/{route}".TrimEnd('/');
    }
}

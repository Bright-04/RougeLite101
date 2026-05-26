public static class RunResultSession
{
    public static bool HasResult { get; private set; }
    public static RunResultType ResultType { get; private set; }
    public static int Stars { get; private set; }
    public static string Summary { get; private set; }

    public static void SetResult(RunResultType resultType, int stars, string summary)
    {
        HasResult = true;
        ResultType = resultType;
        Stars = stars;
        Summary = summary;
    }

    public static void Clear()
    {
        HasResult = false;
        ResultType = default;
        Stars = 0;
        Summary = null;
    }
}

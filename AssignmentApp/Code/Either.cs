namespace AssignmentApp.Code
{
    public record Either<TSome, TErr>(TSome? Some, TErr? Err);
}

using RincewindORMCore;

namespace Test
{
    public class RincewindOrmContext : UnitOfWork
    {
        public Repository<User> Users;

        public RincewindOrmContext(string connectionStrong) : base(connectionStrong)
        {
            Users = Repository<User>();
        }
    }
}

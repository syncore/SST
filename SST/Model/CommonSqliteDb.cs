namespace SST.Model
{
    /// <summary>
    /// Abstract class that defines common SQLite database methods for tool classes.
    /// </summary>
    public abstract class CommonSqliteDb
    {
        /// <summary>
        /// Creates the database.
        /// </summary>
        protected abstract void CreateDb();

        /// <summary>
        /// Checks whether the database file exists.
        /// </summary>
        /// <returns><c>true</c> if the database file exists, otherwise <c>false</c>.</returns>
        protected abstract bool DbExists();

        /// <summary>
        /// Deletes the database.
        /// </summary>
        protected abstract void DeleteDb();

        /// <summary>
        /// Checks where the user exist in database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the user exists in the database, otherwise <c>false</c>.</returns>
        protected abstract bool DoesUserExistInDb(string user);

        /// <summary>
        /// Verifies the database and creates it if it does not exist.
        /// </summary>
        protected abstract bool VerifyDb();
    }
}

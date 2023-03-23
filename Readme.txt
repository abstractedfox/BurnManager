Burn Manager is a simple, lightweight application for offline backups, providing checksums for file integrity verification and using a binpacking algorithm for efficient use of space. Its operation is simple:

1. Choose the "Add Files!" button and select any files you would like to include in your backups. Burn Manager generates an MD5 checksum of every file it tracks, so large amounts of data may take time.

2. In the 'Burns' tab, insert the size of your burn media in bytes, and the cluster size of the format you intend to use. Then, click "Generate Burns!" Files will be sorted to fill each volume tightly, resulting in little wasted space. (Files that are larger than the burn media itself will not be included.)

3. Put a valid directory into "Staging Path". This can be the path to your optical drive, or any other directory on your machine. Click "Stage Burn!", and the files will be copied to this directory, along with a JSON log file describing the original location of every file and its checksum. At this point, you may burn the disc using any software you like.

4. Be sure to save!

This can be a useful part of your backup plan, as burned media is safe from software related corruption, ransomware, and some forms of user error.

Currently this application is in an MVP state; the above features all work cleanly and without apparent issues, but do let me know if you discover any.

Under the hood, the architectural goals were for Burn Manager to have fairly clean separation of concerns, to make sensible use of generic design, and to keep to fairly modular, reusable code throughout. All the major program logic and data exists in the "BurnManager" project and its classes, such that any UI implementation should only need to create an instance of BurnManagerAPI and use the available functions as documented. The "BurnManager" project is also intended to be platform independent, and was shown to work with no changes on MacOS at various points in development.

The "BurnManagerFront" project is the included WPF frontend, and per the goals above, only contains logic or functionality specific to frontend behaviors.

This release of BurnManager is a full rewrite of the original Burn Manager (also known as "DiscDoingsWPF"). That project performed the same base functionality as this one, but was started as a learning exercise for C#, WPF, and asynchronous programming all at once. Users will find this release to be significantly more performant, and programmers will find it much easier to read and modify.
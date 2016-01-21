﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace fqopy
{
	[Cmdlet( VerbsCommon.Copy, "Files" )]
	[CmdletBinding]
	public class CopyFiles : Cmdlet
	{
		[Parameter( Mandatory = true, Position = 0 )]
		public string Source
		{
			get { return source; }
			set { source = value.TrimEnd( new char[] { '\\', '/' } ); }
		}
		string source;

		[Parameter( Mandatory = true, Position = 1 )]
		public string Destination
		{
			get { return destination; }
			set { destination = value.TrimEnd( new char[] { '\\', '/' } ); }
		}
		string destination;

		[Parameter( Mandatory = false, Position = 2 )]
		public string Filter
		{
			get { return filter; }
			set { filter = value; }
		}
		string filter = "*";

		[Parameter( Mandatory = false )]
		public SwitchParameter Recurse { get; set; }

		[Parameter( Mandatory = false )]
		public SwitchParameter Overwrite { get; set; }

		[Parameter( Mandatory = false )]
		public SwitchParameter Fast { get; set; }

		[Parameter( Mandatory = false )]
		public SwitchParameter ShowProgress { get; set; }

		[Parameter( Mandatory = false )]
		public string List { get; set; }

		[Parameter( Mandatory = false )]
		public SwitchParameter PassThru { get; set; }

		IEnumerable<string> filesToCopy    = new List<string>();
		IEnumerable<string> listOfDestDirs = new List<string>();
		int countOfFiles = 0;
		Crc32 crc32 = new Crc32();


		protected override void BeginProcessing()
		{
			try
			{
				if ( string.IsNullOrEmpty( List ) )
				{
					var so = Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
					filesToCopy = Directory.GetFiles( Source, Filter, so );
				}
				else
				{
					filesToCopy = ( File.ReadAllLines( List ) ).Select( path => Path.GetFullPath( Source + path ) );
				}
			}
			catch ( ArgumentException ex )
			{
				WriteError( new ErrorRecord( ex, "1", ErrorCategory.InvalidArgument, Source ) );
			}
			catch ( DirectoryNotFoundException ex )
			{
				WriteError( new ErrorRecord( ex, "2", ErrorCategory.ObjectNotFound, Source ) );
			}
			catch ( IOException ex )
			{
				WriteError( new ErrorRecord( ex, "3", ErrorCategory.ReadError, Source ) );
			}
			catch ( UnauthorizedAccessException ex )
			{
				WriteError( new ErrorRecord( ex, "4", ErrorCategory.PermissionDenied, Source ) );
			}

			if ( filesToCopy != null )
			{
				listOfDestDirs = filesToCopy.Select( path => Path.GetDirectoryName( path.Replace( Source, Destination ) ) ).Distinct();
				countOfFiles = filesToCopy.Count();
			}
		}

		protected override void EndProcessing()
		{
			if ( filesToCopy != null )
			{
				int i = 0;
				var progress = new ProgressRecord( 0, string.Format( "Copy from {0} to {1}", Source, Destination ), "Copying" );
				var startTime = DateTime.Now;

				foreach ( string dir in listOfDestDirs )
				{
					try
					{
						if ( !Directory.Exists( dir ) )
						{
							Directory.CreateDirectory( dir );
						}
					}
					catch ( UnauthorizedAccessException ex )
					{
						WriteVerbose( ex.Message );
					}
					catch ( PathTooLongException ex )
					{
						WriteVerbose( ex.Message );
					}
					catch ( ArgumentNullException ex )
					{
						WriteVerbose( ex.Message );
					}
					catch ( ArgumentException ex )
					{
						WriteVerbose( ex.Message );
					}
					catch ( DirectoryNotFoundException ex )
					{
						WriteVerbose( ex.Message );
					}
					catch ( NotSupportedException ex )
					{
						WriteVerbose( ex.Message );
					}
					catch ( IOException ex )
					{
						WriteVerbose( ex.Message );
					}
				}

				foreach ( var item in CopyFilesUtility.CopyFiles( Source, Destination, filesToCopy, Fast ) )
				{
					if ( !string.IsNullOrEmpty( item.ErrorMessage ) )
					{
						WriteVerbose( item.ErrorMessage );
					}

					if ( PassThru )
					{
						WriteObject( item );
					}

					if ( ShowProgress )
					{
						int percentage = (int) ( (double) ++i / filesToCopy.Count() * 100 );
						progress.PercentComplete = percentage <= 100 ? percentage : 100;
						progress.SecondsRemaining = (int) ( ( ( DateTime.Now - startTime ).TotalSeconds / i ) * ( countOfFiles - i ) );
						WriteProgress( progress );
					}
				}

				if ( ShowProgress )
				{
					progress.RecordType = ProgressRecordType.Completed;
					progress.PercentComplete = 100;
					WriteProgress( progress );
				}
			}
		}
	}
}

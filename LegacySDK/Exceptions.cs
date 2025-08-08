using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	public class InvalidRecord : Exception
	{ public InvalidRecord() : base( "Attempt to navigate to an invalid record." ) {} }

	public class UnknownResponseCode : Exception
	{ public UnknownResponseCode() : base( "Unknown response code." ) {} }

	public class InvalidFieldUpdate : Exception
	{ public InvalidFieldUpdate() : base( "Invalid field update." ) { } }

	public class InvalidHandle : Exception
	{ public InvalidHandle() : base( "Invalid handle." ) { } }

	public class RecordNotFound : Exception
	{ public RecordNotFound() : base( "Record not found." ) { } }

	public class BadMessage : Exception
	{ public BadMessage() : base( "Invalid message." ) { } }

	public class BadResponse : Exception
	{ public BadResponse() : base( "Invalid response." ) { } }

	public class InvalidAtEoT : Exception
	{ public InvalidAtEoT() : base( "Operation not supported at End of Table." ) { } }

	public class UnknownField : Exception
	{ public UnknownField() : base( "Unknown field." ) { } }

	public class InvalidArgument : Exception
	{ public InvalidArgument() : base( "Invalid argument." ) { } }

	public class NavigationWithPendingUpdates : Exception
	{ public NavigationWithPendingUpdates() : base( "Attempt to navigate off a record with pending updates." ) { } }

}
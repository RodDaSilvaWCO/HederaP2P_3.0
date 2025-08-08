using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	public enum APIOperation
	{
		FIRST								= 0,

		HeartBeat							= 0,
		CreateSession						= 1,
		UserLogin							= 10,
		UserLogout							= 11,
		AppLogin							= 12,
		AppLogout							= 13,

		CreateSet							= 20,
		FindSet								= 24,
		OpenSet								= 25,
		CloseSet							= 26,
		ReadSet								= 27,
		WriteSet							= 28,
		DeleteSet							= 29,
		MoveOrRenameSet						= 30,
		GetProperty							= 31,
		SetProperty							= 32,
		GetFullPath							= 33,

		GetTableFieldByName					= 40,
		GetTableFieldByOrdinal				= 41,
		PutTableFieldByName					= 42,
		PutTableFieldByOrdinal				= 43,

		WriteTableRecord					= 44,
		ReadTableRecord						= 45,

		MoveNext							= 50,
		GoTo								= 51,
		GoTop								= 52,
		GoBottom							= 53,

		LAST								= 53
	}
	public enum APIOperationResponseCode
	{
		FIRST							= -25,

		INVALID_AT_EOT					= -25,

		INVALID_RECORD_ORDINAL			= -24,
		RECORD_NOT_FOUND				= -23,
		NAVIGATION_WITH_PENDING_UPDATES = -22,
		UNKNOWN_FIELD					= -20,
		INVALID_HANDLE					=- 10,
		BAD_RESPONSE					= -3,
		BAD_MESSAGE						= -2,
		INVALID_ARGUMENT				= -1,
		OK								=  0,
		FALSE							=  1,

		LAST							=  1
	}


}
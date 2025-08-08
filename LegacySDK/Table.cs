using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosys.Common.Types;
using Unosys.Common;
using UnoSys.Api.Exceptions;
using UnoSys.Api.Models;
using Unosys.Kernel;
using System.Dynamic;

namespace Unosys.SDK
{
	public class Table : DynamicObject, IDisposable
	{
		#region Member Fields
		// NOTE:  Following IsBoT & isEoT state is undefined - the two can never be true at the same time.
		private uint setMode = 0;   
        private bool isBoT = true;
		private bool isEoT = true;
		private bool isDeleted = false;		// default
		private bool appendMode = false;	// default
		private bool autoCommit = false;	// deafult
		private bool isDirty = false;		// default
		HandleWithFieldDefs? handleWithFieldDefs = null!;
		private ulong recordOrdinal = 0;
		private ulong lastRecordOrdinal = 0;
		private Dictionary<string, int>? fieldName2fieldOrdinalMap = new Dictionary<string, int>();
		private Dictionary<int, Field>? fieldOrdinal2fieldDefinition = new Dictionary<int, Field>();
		private TypedSetMember? typedSetMember = null!;
		private Dictionary<string, SortedList<string, List<ulong>>> orders = new Dictionary<string, SortedList<string,List<ulong>>>();  //  used to store the orders for the table
		private int currentOrder = 0;  // used to keep track of the current order being used for the table - default is 0 (i.e.; no order applied yet)
		private string currentOrderKey = null!;
		private int currentOrderRecordOrdinalPos = -1;
        #endregion

        #region Public Events
        public delegate void UnCommittedRecord( object sender, TableEventArgs e );

		public event UnCommittedRecord? NavigateOffDirtyRecord;
		#endregion

		#region Constructors
		#region CreateTable constructors
		private Table(string absoluteTableName, Unosys.Common.Types.SetSchema tableSchema, uint setmode, uint desiredAccess, uint setAttributes, uint shareMode)
        {
			setMode = setmode;
            handleWithFieldDefs = API.CreateTableSetAsync(absoluteTableName, tableSchema, setmode, desiredAccess, setAttributes, shareMode).Result;
            InitCache();
            InitDefaultEvents();
            // After creating a new table there are no records so EoT is true 
            isEoT = true;
            isBoT = false;  // NOTE:  only true if an attempt is made to navigate "backwards" from the first record
            appendMode = false;
            recordOrdinal = handleWithFieldDefs.InitialRecordOrdinal;  // Should always be 0 since we just created the record
            isDeleted = handleWithFieldDefs.IsDeleted;
            // After creating a new table we are on the EoT record which is never dirty (unless appendMode = true)
            isDirty = false;
            lastRecordOrdinal = 0;  // undefined if no records
			typedSetMember = new TypedSetMember(1, handleWithFieldDefs.TableSchema, /* %TODO% */64 * 1024);
            typedSetMember.Ordinal = lastRecordOrdinal;  // Newly created table has no records so ordinal is 0 (i.e.; EoT record)
			typedSetMember.IsDirty = isDirty;  // Newly created table has no records so the record is not dirty
			InitAllOrdersSchema();
        }
        #endregion

        #region OpenTable constructor
        private Table(string absoluteTableName, uint setMode, uint desiredAccess, uint setAttributes, uint shareMode)
		{
			handleWithFieldDefs = API.OpenTableSetAsync(absoluteTableName, setMode, desiredAccess, setAttributes, shareMode).Result;
			InitCache();
			InitDefaultEvents();
			// When opening a table we are passed back the initial record ordinal of the record we are on - if 0 we are EoT
			recordOrdinal = handleWithFieldDefs.InitialRecordOrdinal;
			lastRecordOrdinal = handleWithFieldDefs.LastRecordOrdinal;
			isDeleted = handleWithFieldDefs.IsDeleted;
			if (recordOrdinal == 0)
			{
				isEoT = true;
				isBoT = false; // NOTE:  only true if an attempt is made to navigate "backwards" from the first record
			}
			else
			{                
                isEoT = false;
				isBoT = false;  // NOTE:  only true if an attempt is made to navigate "backwards" from the first record
			}
			appendMode = false;
			// After opening a table we are either on the EoT record which is never dirty (unless appendMode = true), or the freshly read (i.e.; not dirty) first logical record
			isDirty = false;
			typedSetMember = new TypedSetMember(1, handleWithFieldDefs.TableSchema, /*TODO% */64 * 1024);
            typedSetMember.Ordinal = recordOrdinal;  
            typedSetMember.IsDirty = isDirty;
			ReadSetMember();
			InitAllOrdersSchema();
			ReadAllOrders();
        }
		#endregion
		#endregion

		#region IDisposable Implementation
		public void Dispose()
        {
            if (handleWithFieldDefs != null)
            {
                Close();
            }
        }
        #endregion 


        #region Public Properties
        public bool AutoCommit
		{
			get	{	return autoCommit;	}
			set	{	autoCommit = value; }
		}

		public bool IsDeleted
		{
			get { return isDeleted;  }
		}

		public bool IsDirty
		{
			get { return isDirty; }
		}

		public bool IsEoT
		{
			get	{ return isEoT;	 }
			set	{ isEoT = value; }
		}

		public ulong LastRecord
		{
			get
			{
				return lastRecordOrdinal;
			}
		}

		public bool IsBoT
		{
			get	{ return isBoT;  }
			set	{ isBoT = value; }
		}

		public ulong RecordOrdinal
		{
			get
			{
				if (!isEoT)
				{
					return recordOrdinal;
				}
				else  // must be on EoT
				{
					if (lastRecordOrdinal == 0)   // No records?
					{
						return 0;  // lastOrdinal is undefined
					}
					else // there is at least 1 record
					{
						return lastRecordOrdinal + 1;  // EoT
					}
				}
			}
		}

		public long Handle
		{
			get	{ return handleWithFieldDefs!.Handle; }
		}

		public int FieldCount
        {
            get
            {
                if (handleWithFieldDefs!.TableSchema!.Fields == null)
                {
                    throw new InvalidOperationException("Table schema is not defined.");
                }
                return handleWithFieldDefs.TableSchema!.Fields.Count - 1;  // -1 because we do not count the RID field (i.e.; field 0) in the field count
            }
        }	

        #region Field Access (Indexer) Properties
		public override bool TryGetMember(GetMemberBinder binder, out object? result)
		{
			result = this[binder.Name.ToUpper()];
			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder, object? value)
		{
			this[binder.Name.ToUpper()] = value!;
			return true;
        }



        // This indexer allows for syntax like Table["FieldName1"] to return the value of FieldName1 from the current row buffer
        public object this[string fieldName]
		{
			get
			{
				
				try
				{
					return this[fieldName2fieldOrdinalMap![fieldName.ToUpper()]];
				}
				catch(Exception ex)
				{
					Debug.Print( ex.ToString() );
					throw new InvalidOperationException( string.Format( "Unknown field '{0}'", fieldName ) );
				}
				
			}

			set
			{

				if (isEoT && !appendMode)
				{
					throw new InvalidOperationException( "EoT recored not updatable if not in append mode." );
				}
				try
				{
					this[fieldName2fieldOrdinalMap![fieldName.ToUpper()]] = value;
				}
				catch
				{
					throw new InvalidOperationException( string.Format( "Unknown field '{0}'", fieldName ) );
				}
			}
		}

		// This indexer allows for syntax like Table[1] to return the value of Field #1 from the current row buffer
		public object this[int fieldOrdinal]
		{
			get
			{
				try
				{
					Field f = fieldOrdinal2fieldDefinition![fieldOrdinal];
					if (f.Value is Uninitialized)
					{
                        // Call over to the Unosys.Processor to obtain the requested field value and cache it
                        // so that the next time it is requested we have it
                        //f.Value = API.GetTableFieldAsync( handleWithFieldDefs!.Handle, f.fieldDef ).Result;
                        f.Value = f.fieldDef.GetObjectFromBytes(typedSetMember!.GetFieldBytes(fieldOrdinal));  // ztrsf the field value in the typedSetMember buffer
                    }
					return f.Value;
				}
				catch
				{
					throw new InvalidOperationException( string.Format( "Invalid field ordinal '{0}'", fieldOrdinal ) );
				}
			}

			set
			{
				if (isEoT && !appendMode)
				{
					throw new InvalidOperationException( "EoT record not updatable if not in Append mode." );
				}
				try
				{
					Field f = fieldOrdinal2fieldDefinition![fieldOrdinal];
                    // We call over to the Unosys.Processor to write the field value to the active buffer
                    // NOTE:  This does NOT cause the record buffer to be written to the store. Records are updated
                    //		  in their entirety when the record is committed. This is why we mark the client side representation
                    //        of the record dirty below, so that we know we still have to "commit" it.
                    //f.Value = API.PutTableFieldAsync( handleWithFieldDefs!.Handle, f.fieldDef, value ).Result;
					//if(fieldValueCache!.ContainsKey(fieldOrdinal))
					//{
					//	fieldValueCache[fieldOrdinal] = value;  // Update the field value in the cache	
     //               }
					//else
					//{
     //                   fieldValueCache.Add(fieldOrdinal, value);  // Add the field value to the cache
     //               }
					_ = typedSetMember!.PutFieldBytes(fieldOrdinal, f.fieldDef.GetBytesFromObject(value));  // Update the field value in the typedSetMember buffer
					f.Value = value;  // Update the field value in the field definition cache
                    isDirty = true;  // we have just updated a field value so we must be dirty
				}
				catch
				{
					throw new InvalidOperationException( string.Format( "Invalid field ordinal '{0}'", fieldOrdinal ) );
				}
			}
		}
		#endregion
		#endregion


		#region Public Methods
		#region Field Access Methods
		public string FieldName(int fieldOrdinal)
		{
			if (fieldOrdinal < 0 || fieldOrdinal > handleWithFieldDefs!.TableSchema!.Fields!.Count)
			{
				throw new InvalidOperationException(string.Format("Invalid Field ordinal '{0}'", fieldOrdinal));
			}
			return handleWithFieldDefs.TableSchema!.Fields![fieldOrdinal].Name;
		}


        public int FieldLen(int fieldOrdinal)
        {
            if (fieldOrdinal < 0 || fieldOrdinal > handleWithFieldDefs!.TableSchema!.Fields!.Count)
            {
                throw new InvalidOperationException(string.Format("Invalid Field ordinal '{0}'", fieldOrdinal));
            }
            return handleWithFieldDefs.TableSchema!.Fields![fieldOrdinal].Length;
        }



        public int FieldLen(string fieldNamel)
        {
            int fieldOrdinal = FieldOrdinal(fieldNamel);
            if (fieldOrdinal < 0 || fieldOrdinal > handleWithFieldDefs!.TableSchema!.Fields!.Count)
            {
                throw new InvalidOperationException(string.Format("Invalid Field ordinal '{0}'", fieldOrdinal));
            }
            return handleWithFieldDefs.TableSchema!.Fields![fieldOrdinal].Length;
        }



        public object Field(int fieldOrdinal)
        {
			return this[fieldOrdinal];
        }

        public object FieldValue(string fieldName)
        {
            return this[fieldName];
        }
        public int FieldOrdinal(string fieldName)
		{
			int fieldOrdinal = 1;
            foreach (var kvp in fieldName2fieldOrdinalMap!)
            {
				if (kvp.Key != "$RID$")  // Skip the RID field
				{
					if (kvp.Key.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
					{
						return fieldOrdinal;
					}
					fieldOrdinal++;
				}
            }
			throw new InvalidOperationException($"Field '{fieldName}' does not exist in the table.");	
        }


        public bool FieldNameExists(string fieldName)
		{
            foreach (var kvp in fieldName2fieldOrdinalMap!)
            {
                if (kvp.Key.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
		}


		public bool OrderNameExists(string orderName)
		{
			if (handleWithFieldDefs!.TableSchema!.Orders == null)
			{
				return false;
			}
			
            if (!FieldNameExists(orderName) )
            {
				return false;  // %TODO% - NOTE:  for now we assume an Order is based on a field name, so if the field name does not exist we return false
            }
			int fieldOrdinal = FieldOrdinal(orderName);  // Get the field ordinal for the order name
            foreach (var kvp in handleWithFieldDefs.TableSchema!.Orders!)
			{
				foreach(var ordFieldOrdinal in kvp.Value.FieldOrdinals)
                {
                    if (ordFieldOrdinal == fieldOrdinal)
                    {
                        return true;  // Found the order name
                    }
                }
            }
			return false;
		}
        #endregion


        #region Record Update methods
        public void Append()
		{
			if( isDirty) // Check if trying to enter Append mode with pending updates
			{
				// Special case if we are already in appendMode and have pending updates
				if( isEoT && appendMode && !autoCommit )
				{
					throw new InvalidOperationException( "Attempt to reenter Append mode with pending updates.  Either Commit or Rollback the updates." );
				}

				if( !autoCommit )
				{
					throw new InvalidOperationException( "Attempt to navigate off a record with uncommitted pending updates." );
				}
				else
				{
					// autoCommit enabled so commit the pending changes before leaving the record
					this.Commit();
				}
			}
			if( !IsEoT )
			{
				// Need to move to EoT
				// %TODO% - below comment out for now
				
				//OrdinalResponse ordResponse = API.GoToAsync( handleWithFieldDefs!.Handle, lastOrdinal + 1 ).Result;
				//ordinal = ordResponse.Ordinal;
				//isEoT = ordResponse.IsEoT;
				//isBoT = ordResponse.IsBoT;
				//isDirty = ordResponse.IsDirty;  // should be false here
				//isDeleted = ordResponse.IsDeleted;
				//lastOrdinal = ordResponse.LastOrdinal;
				ClearCache();
			}
            appendMode = true;  // NOTE:  This must come before the next line so that the record is in appendMode when the new RID is initialized
			typedSetMember!.ReInitializeRecord();  // Reinitialize the record buffer to a new empty record in append mode
            this[0] = Guid.NewGuid().ToString("N").ToUpper();  // Initialize the first field in the record to a unique RID value (e.g.; a GUID) 
		}

		public void Commit()
		{
			if (!isDirty) // Only need to write if dirty...
			{
				throw new InvalidOperationException( "Attempt to Commit a record when there are no pending updates." );
			}
           
            isDirty = false; // For now...
			OrdinalResponse ordResponse = API.WriteTableSetMemberAsync(typedSetMember!, handleWithFieldDefs!, true).Result;
			recordOrdinal = ordResponse.Ordinal;
			isEoT = ordResponse.IsEoT;
			isBoT = ordResponse.IsBoT;
			isDirty = ordResponse.IsDirty;  // should be false here.
			isDeleted = ordResponse.IsDeleted;
			lastRecordOrdinal = ordResponse.LastOrdinal;
			appendMode = false;  // Once a record is committed it can no longer be in appendMode (if was)

            #region Update Orders
            foreach (var kvp in handleWithFieldDefs!.TableSchema!.Orders!)
            {
                if (kvp.Key != 0)  // Ignore the default order (0) which is the order of the table as it was created (i.e.; no order applied yet)
                {
                    var fieldOrdinal = kvp.Value.FieldOrdinals[0];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY
                    //var orderdef = handleWithFieldDefs.TableSchema!.Orders![kvp.Key];
                    Field f = fieldOrdinal2fieldDefinition![fieldOrdinal];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY 
                    if (orders.ContainsKey(f.fieldDef.Name))
                    {
                        SortedList<string, List<ulong>> keyValuePairs = orders[f.fieldDef.Name];
                        if (!keyValuePairs.ContainsKey((string)this[fieldOrdinal]))
                        {
                            keyValuePairs.Add((string)this[fieldOrdinal], new List<ulong> { recordOrdinal });
                        }
                        else
                        {
                            if (!keyValuePairs[(string)this[fieldOrdinal]].Contains(recordOrdinal))
                            {
                                keyValuePairs[(string)this[fieldOrdinal]].Add(recordOrdinal);  // Add the current record ordinal to the order
                            }
                        }
                    }
                }
            }
            #endregion
        }

        public void Rollback()
		{
			if( !isDirty )
			{
				throw new InvalidOperationException( "Attempt to Rollback a record when there are no pending updates." );
			}
			// NOTE:  We need to reread  (or in the case of EoT reinitialize) the current record
			OrdinalResponse ordResponse = API.GoToAsync( handleWithFieldDefs!.Handle, recordOrdinal, true ).Result;  // true:  part of a Rollback operation
			recordOrdinal = ordResponse.Ordinal;
			isEoT = ordResponse.IsEoT;
			isBoT = ordResponse.IsBoT;
			isDirty = ordResponse.IsDirty;  // should be false here
			isDeleted = ordResponse.IsDeleted;
			lastRecordOrdinal = ordResponse.LastOrdinal;
			ClearCache();
			appendMode = false;  // Once a record is Rolled back it can no longer be in appendMode (if was)
		}
		#endregion

		#region Order Methods
		public int SetOrder(int orderOrdinal)
		{
            // NOTE:  This method should not change the current recordOrdinal value nor move of the current record.  It should only set the currentOrder, currentOrderKey and currentORderRecordOrdinalPos.
            if (orderOrdinal < 0 || orderOrdinal >= handleWithFieldDefs!.TableSchema!.Orders!.Count)
			{
				throw new InvalidOperationException(string.Format("Invalid Order ordinal '{0}'", orderOrdinal));
			}
			var oldOrder = currentOrder;  // Save the old order before changing it	
			currentOrder = orderOrdinal;
			//if (!orders.ContainsKey(handleWithFieldDefs!.TableSchema!.Orders![currentOrder].FieldDef.Name))
			//{
			//    throw new InvalidOperationException(string.Format("Order '{0}' is not defined for this table.", handleWithFieldDefs.TableSchema!.Orders![currentOrder].FieldDef.Name));
			//}
			if (currentOrder != 0)  // If the order is not 0 (natural order) then we need to position ourselves in selected order 
			{
				var orderDef = handleWithFieldDefs.TableSchema!.Orders![currentOrder];
				var fieldOrdinal = orderDef.FieldOrdinals[0];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY
				Field f = fieldOrdinal2fieldDefinition![fieldOrdinal];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY 
				SortedList<string, List<ulong>> keyValuePairs = orders[f.fieldDef.Name];
				#region Locate the current record in the order
				foreach (var kvp in keyValuePairs)
				{
					if (kvp.Value.Contains(recordOrdinal))
					{
						// We found the current record in the order, now we need to position ourselves on it
						// NOTE:  We do not need to read the record again since we are already positioned on it
						int recOrdPos = 0;
						foreach (var recOrd in kvp.Value)
						{
							if (recOrd == recordOrdinal)
							{
								currentOrderKey = kvp.Key;  // Save the current order key so we can use it later
								currentOrderRecordOrdinalPos = recOrdPos;  // Save the position of the current record in the order
								break;  // We found the current recordOrdinal in the order, no need to continue searching
							}
							recOrdPos++;
						}
					}
				}
				#endregion
			}
			else
			{
				// We are switching to natural order
				currentOrderKey = null!;
                currentOrderRecordOrdinalPos = -1;  // Reset the position of the current record in the order
            }
            return oldOrder;   // returns the previous order
		}

		public int SetOrder(string orderName)
		{
			bool orderFound = false;
			int orderOrdinal = 0;  // Default to 0 (natural order) if not found	
			foreach (var kvp in handleWithFieldDefs!.TableSchema!.Orders!)
			{
				int fieldOrdinal = kvp.Value.FieldOrdinals[0];
				if (fieldOrdinal != 0)
				{
					if (FieldName(fieldOrdinal).Equals(orderName, StringComparison.OrdinalIgnoreCase))
					{
						orderFound = true;
						orderOrdinal = kvp.Key;
						break;
					}
				}
			}
			if (orderFound)
			{
				return SetOrder(orderOrdinal);
			}
			else
			{
				throw new InvalidOperationException(string.Format("Order '{0}' is not defined for this table.", orderName));
			}
		}
        #endregion

        #region Navigation methods
        public ulong GoTo( ulong recordOrdinal )
		{
			if (isDirty)
			{
				if (!autoCommit)
				{
					throw new InvalidOperationException( "Attempt to navigate off a record with uncommitted pending updates." );
				}
				else
				{
					// autoCommit enabled so commit the pending changes before leaving the record
					this.Commit();
				}
			}
			// NOTE:  We need to reread  (or in the case of EoT reinitialize) the record
			OrdinalResponse ordResponse = API.GoToAsync( handleWithFieldDefs!.Handle, recordOrdinal ).Result;
			this.recordOrdinal = ordResponse.Ordinal;
			isEoT = ordResponse.IsEoT;
			isBoT = ordResponse.IsBoT;
			isDirty = ordResponse.IsDirty;  // should be false here
			isDeleted = ordResponse.IsDeleted;
			lastRecordOrdinal = ordResponse.LastOrdinal;
			ClearCache();
			return this.recordOrdinal;
		}

		public ulong GoTop()
		{
			if (isDirty)
			{
				if (!autoCommit)
				{
					throw new InvalidOperationException( "Attempt to navigate off a record with uncommitted pending updates." );
				}
				else
				{
					// autoCommit enabled so commit the pending changes before leaving the record
					this.Commit();
				}
			}
			OrdinalResponse ordResponse = null!; // API.GoTopAsync( handleWithFieldDefs ).Result;
			
			if (lastRecordOrdinal != 0)
			{
				if (currentOrder == 0)
				{
					// If we make it here we are in natural order and there are records in the table
					typedSetMember!.Ordinal = 1;  // Set the typedSetMember ordinal to the first record ordinal
					ordResponse = API.ReadTableSetMemberAsync(typedSetMember!, handleWithFieldDefs!).Result;
				}
				else
				{
                    // If we make it here we are in an order and there are records in the table
                    var orderDef = handleWithFieldDefs!.TableSchema!.Orders![currentOrder];
                    var fieldOrdinal = orderDef.FieldOrdinals[0];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY
                    Field f = fieldOrdinal2fieldDefinition![fieldOrdinal];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY 
                    SortedList<string, List<ulong>> keyValuePairs = orders[f.fieldDef.Name];
                    //ordResponse = API.TableOrderGoTop(typedSetMember!, handleWithFieldDefs!, currentOrder, keyValuePairs, ref currentOrderKey, ref currentOrderRecordOrdinalPos);
                    var kvp = keyValuePairs.ElementAt(0);
                    currentOrderKey = kvp.Key; // First logical Key
                    currentOrderRecordOrdinalPos = 0; // First logical record ordinal position in the order
                    typedSetMember!.Ordinal = kvp.Value[0];  // Set the typedSetMember ordinal to the first record ordinal in the order
                    ordResponse = API.ReadTableSetMemberAsync(typedSetMember!, handleWithFieldDefs!).Result;
                }
                recordOrdinal = ordResponse.Ordinal;
                isEoT = ordResponse.IsEoT;
                isBoT = ordResponse.IsBoT;
                isDirty = ordResponse.IsDirty;  // should be false here
                isDeleted = ordResponse.IsDeleted;
                lastRecordOrdinal = ordResponse.LastOrdinal;
                ClearCache();
            }
			else  // There are no records (i.e.; we are on EoT)
			{
				// We are on the EoT record, so we need to reinitialize the typedSetMember to an empty record
				typedSetMember!.ReInitializeRecord();  // Reinitialize the record buffer to a new empty record in append mode
				recordOrdinal = 0;  // No records in the table so we are on EoT
				isEoT = true;
				isDirty = false;
				isDeleted = false;
				ClearCache();
			}
            return recordOrdinal;
		}



        public ulong SeekExact(string orderKey )
        {
			if( currentOrder == 0 )
			{
                throw new InvalidOperationException("Attempt to Seek while in natural order.");
            }

            if (isDirty)
            {
                if (!autoCommit)
                {
                    throw new InvalidOperationException("Attempt to navigate off a record with uncommitted pending updates.");
                }
                else
                {
                    // autoCommit enabled so commit the pending changes before leaving the record
                    this.Commit();
                }
            }
            OrdinalResponse ordResponse = null!; // API.GoTopAsync( handleWithFieldDefs ).Result;

            if (lastRecordOrdinal != 0)
            {
                // If we make it here we are in an order and there are records in the table
                var orderDef = handleWithFieldDefs!.TableSchema!.Orders![currentOrder];
                var fieldOrdinal = orderDef.FieldOrdinals[0];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY
                Field f = fieldOrdinal2fieldDefinition![fieldOrdinal];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY 
                SortedList<string, List<ulong>> keyValuePairs = orders[f.fieldDef.Name];
                if (!keyValuePairs.ContainsKey(orderKey))
                {
                    // We didn't find the key in the order, so we need to position ourselves at EoT
                    typedSetMember!.ReInitializeRecord();  // Reinitialize the record buffer to a new empty record in append mode
                    isEoT = true;
                    recordOrdinal = 0;  //  we are on EoT
                    isBoT = false;
					isDirty = false;
                    isDeleted = false;
					currentOrderKey = null!;
                    currentOrderRecordOrdinalPos = -1;  // Reset the position of the current record in the order	
                    ClearCache();
                }
                else
				{
                    currentOrderKey = orderKey; // Set the current order key to the requested order key
                    currentOrderRecordOrdinalPos = 0; // Reset the position of the current record in the order
                    typedSetMember!.Ordinal = keyValuePairs[orderKey][0];  // Set the typedSetMember ordinal to the first record ordinal in the order
                    ordResponse = API.ReadTableSetMemberAsync(typedSetMember!, handleWithFieldDefs!).Result;
                    recordOrdinal = ordResponse.Ordinal;
                    isEoT = ordResponse.IsEoT;
                    isBoT = ordResponse.IsBoT;
                    isDirty = ordResponse.IsDirty;  // should be false here
                    isDeleted = ordResponse.IsDeleted;
                    lastRecordOrdinal = ordResponse.LastOrdinal;
                    ClearCache();
                }
            }
            else  // There are no records (i.e.; we are on EoT)
            {
                // We are on the EoT record, so we need to reinitialize the typedSetMember to an empty record
                typedSetMember!.ReInitializeRecord();  // Reinitialize the record buffer to a new empty record in append mode
                                                       // We didn't find the key in the order, so we need to position ourselves at EoT
                isEoT = true;
                recordOrdinal = 0;  //  we are on EoT
                isBoT = false;
                isDirty = false;
                isDeleted = false;
                currentOrderKey = null!;
                currentOrderRecordOrdinalPos = -1;  // Reset the position of the current record in the order	
                ClearCache();
            }
            return recordOrdinal;
        }


        public ulong SeekPartial(string orderKey)
        {
			// NOTE:  This method does the search case insensitive
            if (currentOrder == 0)
            {
                throw new InvalidOperationException("Attempt to Seek while in natural order.");
            }

            if (isDirty)
            {
                if (!autoCommit)
                {
                    throw new InvalidOperationException("Attempt to navigate off a record with uncommitted pending updates.");
                }
                else
                {
                    // autoCommit enabled so commit the pending changes before leaving the record
                    this.Commit();
                }
            }
            OrdinalResponse ordResponse = null!; // API.GoTopAsync( handleWithFieldDefs ).Result;

            if (lastRecordOrdinal != 0)
            {
                // If we make it here we are in an order and there are records in the table
                var orderDef = handleWithFieldDefs!.TableSchema!.Orders![currentOrder];
                var fieldOrdinal = orderDef.FieldOrdinals[0];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY
                Field f = fieldOrdinal2fieldDefinition![fieldOrdinal];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY 
                SortedList<string, List<ulong>> keyValuePairs = orders[f.fieldDef.Name];
                // Find the nearest key in the order that is greater than or equal to the requested orderKey
                if (!keyValuePairs.ContainsKey(orderKey))
                {
					bool found = false;
					foreach (var kvp in keyValuePairs)
					{
						if (kvp.Key.Substring(0, orderKey.Length).Equals( orderKey,  StringComparison.OrdinalIgnoreCase) )
						{
							// We found a key that is greater than or equal to the requested orderKey
							currentOrderKey = kvp.Key; // Set the current order key to the found key
							currentOrderRecordOrdinalPos = 0; // Reset the position of the current record in the order
							typedSetMember!.Ordinal = kvp.Value[0];  // Set the typedSetMember ordinal to the first record ordinal in the order
							ordResponse = API.ReadTableSetMemberAsync(typedSetMember!, handleWithFieldDefs!).Result;
							recordOrdinal = ordResponse.Ordinal;
							isEoT = ordResponse.IsEoT;
							isBoT = ordResponse.IsBoT;
							isDirty = ordResponse.IsDirty;  // should be false here
							isDeleted = ordResponse.IsDeleted;
							lastRecordOrdinal = ordResponse.LastOrdinal;
							ClearCache();
							found = true;
							break;  // Return the record ordinal of the found record
						}
					}
					if (!found)
					{
						// We didn't find the key in the order, so we need to position ourselves at EoT
						typedSetMember!.ReInitializeRecord();  // Reinitialize the record buffer to a new empty record in append mode
						isEoT = true;
						recordOrdinal = 0;  //  we are on EoT
						isBoT = false;
						isDirty = false;
						isDeleted = false;
						currentOrderKey = null!;
						currentOrderRecordOrdinalPos = -1;  // Reset the position of the current record in the order	
						ClearCache();
					}
                }
                else
                {
                    currentOrderKey = orderKey; // Set the current order key to the requested order key
                    currentOrderRecordOrdinalPos = 0; // Reset the position of the current record in the order
                    typedSetMember!.Ordinal = keyValuePairs[orderKey][0];  // Set the typedSetMember ordinal to the first record ordinal in the order
                    ordResponse = API.ReadTableSetMemberAsync(typedSetMember!, handleWithFieldDefs!).Result;
                    recordOrdinal = ordResponse.Ordinal;
                    isEoT = ordResponse.IsEoT;
                    isBoT = ordResponse.IsBoT;
                    isDirty = ordResponse.IsDirty;  // should be false here
                    isDeleted = ordResponse.IsDeleted;
                    lastRecordOrdinal = ordResponse.LastOrdinal;
                    ClearCache();
                }
            }
            else  // There are no records (i.e.; we are on EoT)
            {
                // We are on the EoT record, so we need to reinitialize the typedSetMember to an empty record
                typedSetMember!.ReInitializeRecord();  // Reinitialize the record buffer to a new empty record in append mode
                                                       // We didn't find the key in the order, so we need to position ourselves at EoT
                isEoT = true;
                recordOrdinal = 0;  //  we are on EoT
                isBoT = false;
                isDirty = false;
                isDeleted = false;
                currentOrderKey = null!;
                currentOrderRecordOrdinalPos = -1;  // Reset the position of the current record in the order	
                ClearCache();
            }
            return recordOrdinal;
        }


        public ulong SeekClosest(string orderKey)
        {
            // NOTE:  This method does the search case insensitive
            if (currentOrder == 0)
            {
                throw new InvalidOperationException("Attempt to Seek while in natural order.");
            }

            if (isDirty)
            {
                if (!autoCommit)
                {
                    throw new InvalidOperationException("Attempt to navigate off a record with uncommitted pending updates.");
                }
                else
                {
                    // autoCommit enabled so commit the pending changes before leaving the record
                    this.Commit();
                }
            }
            OrdinalResponse ordResponse = null!; // API.GoTopAsync( handleWithFieldDefs ).Result;

            if (lastRecordOrdinal != 0)
            {
                // If we make it here we are in an order and there are records in the table
                var orderDef = handleWithFieldDefs!.TableSchema!.Orders![currentOrder];
                var fieldOrdinal = orderDef.FieldOrdinals[0];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY
                Field f = fieldOrdinal2fieldDefinition![fieldOrdinal];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY 
                SortedList<string, List<ulong>> keyValuePairs = orders[f.fieldDef.Name];
                // Find the nearest key in the order that is greater than or equal to the requested orderKey
                bool found = false;
                foreach (var kvp in keyValuePairs)
                {
					int compareResult = string.Compare(kvp.Key, orderKey, StringComparison.OrdinalIgnoreCase);
                    if (compareResult >= 0)
                    {
                        // We found a key that is greater than or equal to the requested orderKey
                        currentOrderKey = kvp.Key; // Set the current order key to the found key
                        currentOrderRecordOrdinalPos = 0; // Reset the position of the current record in the order
                        typedSetMember!.Ordinal = kvp.Value[0];  // Set the typedSetMember ordinal to the first record ordinal in the order
                        ordResponse = API.ReadTableSetMemberAsync(typedSetMember!, handleWithFieldDefs!).Result;
                        recordOrdinal = ordResponse.Ordinal;
                        isEoT = ordResponse.IsEoT;
                        isBoT = ordResponse.IsBoT;
                        isDirty = ordResponse.IsDirty;  // should be false here
                        isDeleted = ordResponse.IsDeleted;
                        lastRecordOrdinal = ordResponse.LastOrdinal;
                        ClearCache();
                        found = true;
                        break;  // Return the record ordinal of the found record
                    }
                }
                if (!found)
                {
                    // We didn't find the key in the order, so we need to position ourselves at EoT
                    typedSetMember!.ReInitializeRecord();  // Reinitialize the record buffer to a new empty record in append mode
                    isEoT = true;
                    recordOrdinal = 0;  //  we are on EoT
                    isBoT = false;
                    isDirty = false;
                    isDeleted = false;
                    currentOrderKey = null!;
                    currentOrderRecordOrdinalPos = -1;  // Reset the position of the current record in the order	
                    ClearCache();
                }
            }
            else  // There are no records (i.e.; we are on EoT)
            {
                // We are on the EoT record, so we need to reinitialize the typedSetMember to an empty record
                typedSetMember!.ReInitializeRecord();  // Reinitialize the record buffer to a new empty record in append mode
                                                       // We didn't find the key in the order, so we need to position ourselves at EoT
                isEoT = true;
                recordOrdinal = 0;  //  we are on EoT
                isBoT = false;
                isDirty = false;
                isDeleted = false;
                currentOrderKey = null!;
                currentOrderRecordOrdinalPos = -1;  // Reset the position of the current record in the order	
                ClearCache();
            }
            return recordOrdinal;
        }

        public ulong GoBottom()
		{
			if (isDirty)
			{
				if (!autoCommit)
				{
					throw new InvalidOperationException( "Attempt to navigate off a record with uncommitted pending updates." );
				}
				else
				{
					// autoCommit enabled so commit the pending changes before leaving the record
					this.Commit();
				}
			}
			OrdinalResponse ordResponse = API.GoBottomAsync( handleWithFieldDefs!.Handle ).Result;
			recordOrdinal = ordResponse.Ordinal;
			isEoT = ordResponse.IsEoT;
			isBoT = ordResponse.IsBoT;
			isDirty = ordResponse.IsDirty;  // should be false here
			isDeleted = ordResponse.IsDeleted;
			lastRecordOrdinal = ordResponse.LastOrdinal;
			ClearCache();
			return recordOrdinal;
		}

		public ulong MoveNext( int recordsToMove = 1)
		{
			if( isDirty )
			{
				if( !autoCommit )
				{
					throw new InvalidOperationException( "Attempt to navigate off a record with uncommitted pending updates." );
				}
				else
				{
					// autoCommit enabled so commit the pending changes before leaving the record
					this.Commit();
				}
			}
			if (!isEoT)
			{
				OrdinalResponse ordResponse = null!;

                if (currentOrder == 0)
				{
                    ordResponse = API.TableMoveNextAsync(typedSetMember!, handleWithFieldDefs!, recordsToMove).Result;
				}
				else
				{
                    var orderDef = handleWithFieldDefs!.TableSchema!.Orders![currentOrder];
                    var fieldOrdinal = orderDef.FieldOrdinals[0];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY
                    Field f = fieldOrdinal2fieldDefinition![fieldOrdinal];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY 
                    SortedList<string, List<ulong>> keyValuePairs = orders[f.fieldDef.Name];
                    ordResponse = API.TableOrderMoveNext(typedSetMember!, handleWithFieldDefs!, currentOrder, keyValuePairs, recordsToMove, ref currentOrderKey, ref currentOrderRecordOrdinalPos);
                }
				recordOrdinal = ordResponse.Ordinal;
				isEoT = ordResponse.IsEoT;
				isBoT = ordResponse.IsBoT;
				isDirty = ordResponse.IsDirty;  // should be false here
				isDeleted = ordResponse.IsDeleted;
				lastRecordOrdinal = ordResponse.LastOrdinal;
				ClearCache();
			}
			return recordOrdinal;
		}
		#endregion

		#region Other Methods
		public void Close()
		{
			if (handleWithFieldDefs != null)
			{
				if (isDirty)
				{
					if (!autoCommit)
					{
						throw new InvalidOperationException("Attempt to navigate off a record with uncommitted pending updates.");
					}
					else
					{
						// autoCommit enabled so commit the pending changes before leaving the record
						this.Commit();
					}
				}

				#region Write Orders only if closing the table while in Create Mode
				if (setMode == 2 /*Create*/)
				{
					byte[][] orderBytes = new byte[orders.Count + 1][];  // +1 for the extra natural order place holder that will be ignored (i.e.; order 0)
					int orderIndex = 0;  
                    foreach (var orderDef in handleWithFieldDefs!.TableSchema!.Orders!)
					{
						if (orderDef.Key != 0)  // Ignore the default order (0) which is the order of the table as it was created (i.e.; natural order)
						{
							// Allocate buffer to hold all keys of order and their record ordinals
							var fieldOrdinal = orderDef.Value.FieldOrdinals[0];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY
							Field f = fieldOrdinal2fieldDefinition![fieldOrdinal];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY 
							var orderKeyLength = f.fieldDef.Length;
							orderBytes[orderIndex] = new byte[lastRecordOrdinal * (ulong)(orderKeyLength + sizeof(ulong))];
							// Now fill the buffer 
							SortedList<string, List<ulong>> keyValuePairs = orders[f.fieldDef.Name];
							int orderEntryPos = 0;
							int uniqueRecCount = 0;
							foreach (var uniqueKeyPair in keyValuePairs)
							{
								string orderKey = uniqueKeyPair.Key.PadRight(f.fieldDef.Length);
								var keyBytes = Encoding.ASCII.GetBytes(orderKey);
								foreach (var uniqueRecordOrdinal in uniqueKeyPair.Value)
								{
									Buffer.BlockCopy(keyBytes, 0, orderBytes[orderIndex], orderEntryPos, keyBytes.Length);  // Copy the key to the buffer
									orderEntryPos += keyBytes.Length;  // Move the position in the buffer to the end of the key
									Buffer.BlockCopy(BitConverter.GetBytes(uniqueRecordOrdinal), 0, orderBytes[orderIndex], orderEntryPos, sizeof(ulong));  // Copy the record ordinal to the buffer
									orderEntryPos += sizeof(ulong);  // Move the position in the buffer to the end of the record ordinal
									uniqueRecCount++;

								}
							}
							Console.WriteLine($"Creating Index on field: {f.fieldDef.Name}...");
							API.WriteTableOrderAsync(handleWithFieldDefs.Handle, Convert.ToUInt64(orderDef.Key), orderBytes[orderIndex]).Wait();  // Write the order to the store
							orderIndex++;  // Increment the order index for the next order
                        }
						//break;  // %TODO%  Only write one Order for now...
					}
				}
                #endregion

                var notUsed = API.CloseTableSetAsync(handleWithFieldDefs).Result;

                handleWithFieldDefs = null!;
				if (fieldName2fieldOrdinalMap != null)
				{
					fieldName2fieldOrdinalMap.Clear();
					fieldName2fieldOrdinalMap = null!;
				}
				if (fieldOrdinal2fieldDefinition != null)
				{
					fieldOrdinal2fieldDefinition.Clear();
					fieldOrdinal2fieldDefinition = null!;
				}
				NavigateOffDirtyRecord -= TableNavigateOffDirtyRecord;
			}
		}
        #endregion

        #endregion


        #region Private Helpers
        private void ReadAllOrders()
        {
            if (handleWithFieldDefs != null)
            {
                #region Read Orders
                byte[][] orderBytes = new byte[orders.Count+1][];    // +1 for the extra natural order place holder that will be ignored (i.e.; order 0)
                foreach (var orderDef in handleWithFieldDefs!.TableSchema!.Orders!)
                {
                    if (orderDef.Key != 0)  // Ignore the default order (0) which is the order of the table as it was created (i.e.; natural order)
                    {
                        // Allocate buffer to hold all keys of order and their record ordinals
                        var fieldOrdinal = orderDef.Value.FieldOrdinals[0];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY
                        Field f = fieldOrdinal2fieldDefinition![fieldOrdinal];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY 
                        var orderKeyLength = f.fieldDef.Length;
                        orderBytes[orderDef.Key] = orderBytes[orderDef.Key] = API.ReadTableOrderAsync(handleWithFieldDefs.Handle, Convert.ToUInt64(orderDef.Key)).Result;  // Read the order from the store
                        // Now fill the buffer 
                        SortedList<string, List<ulong>> keyValuePairs = orders[f.fieldDef.Name];
                        int orderEntryPos = 0;
                        ulong uniqueRecCount = 0;
						while (uniqueRecCount < handleWithFieldDefs!.LastRecordOrdinal)
						{
							// Read the key from the buffer
							string orderKey = Encoding.ASCII.GetString(orderBytes[orderDef.Key], orderEntryPos, f.fieldDef.Length);
							orderEntryPos += f.fieldDef.Length;  // Move the position in the buffer to the end of the key
																 // Read the record ordinal from the buffer
							ulong uniqueRecordOrdinal = BitConverter.ToUInt64(orderBytes[orderDef.Key], orderEntryPos);
							orderEntryPos += sizeof(ulong);  // Move the position in the buffer to the end of the record ordinal
							if (!keyValuePairs.ContainsKey(orderKey))
							{
								keyValuePairs.Add(orderKey, new List<ulong> { uniqueRecordOrdinal });
							}
							else
							{
								if (!keyValuePairs[orderKey].Contains(uniqueRecordOrdinal))
								{
									keyValuePairs[orderKey].Add(uniqueRecordOrdinal);  // Add the current record ordinal to the order
								}
							}
							uniqueRecCount++;
                        }
                        //Console.WriteLine($"Table.ReadAllOrders() Order: {f.fieldDef.Name}, uniqueRecCount={uniqueRecCount}, keyLength={f.fieldDef.Length},  orderEntryPos={orderEntryPos}, bufferSize={orderBytes[orderDef.Key].Length}");
                        //break;  // %TODO%  Only Read one Order for now...
                    }
                    
                }
                #endregion
            }
        }


     //   private void ReadOrder(int currentOrder, bool isDescending = false)
     //   {
     //       if (handleWithFieldDefs != null)
     //       {
     //           #region Read Orders
     //           //Dictionary<int,OrderDef>? orderDef = handleWithFieldDefs!.TableSchema!.Orders![currentOrder];
     //           byte[][] orderBytes = new byte[orders.Count + 1][];    // +1 for the extra natural order place holder that will be ignored (i.e.; order 0)
     //           foreach (var orderDef in handleWithFieldDefs!.TableSchema!.Orders!)
     //           {
					//if (orderDef.Key == currentOrder)  // Read only the current order
					//{
					//	if (orderDef.Key != 0)  // Ignore the default order (0) which is the order of the table as it was created (i.e.; natural order)
					//	{
					//		// Allocate buffer to hold all keys of order and their record ordinals
					//		var fieldOrdinal = orderDef.Value.FieldOrdinals[0];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY
					//		Field f = fieldOrdinal2fieldDefinition![fieldOrdinal];  // %TODO%  for now assuming each Order expression is based on a single field ordinal ONLY 
					//		var orderKeyLength = f.fieldDef.Length;
					//		orderBytes[orderDef.Key] = orderBytes[orderDef.Key] = API.ReadTableOrderAsync(handleWithFieldDefs.Handle, Convert.ToUInt64(orderDef.Key)).Result;  // Read the order from the store
					//																																						   // Now fill the buffer 
					//		SortedList<string, List<ulong>> keyValuePairs = orders[f.fieldDef.Name];
					//		int orderEntryPos = 0;
					//		ulong uniqueRecCount = 0;
					//		while (uniqueRecCount < handleWithFieldDefs!.LastRecordOrdinal)
					//		{
					//			// Read the key from the buffer
					//			string orderKey = Encoding.ASCII.GetString(orderBytes[orderDef.Key], orderEntryPos, f.fieldDef.Length);
					//			orderEntryPos += f.fieldDef.Length;  // Move the position in the buffer to the end of the key
					//												 // Read the record ordinal from the buffer
					//			ulong uniqueRecordOrdinal = BitConverter.ToUInt64(orderBytes[orderDef.Key], orderEntryPos);
					//			orderEntryPos += sizeof(ulong);  // Move the position in the buffer to the end of the record ordinal
					//			if (!keyValuePairs.ContainsKey(orderKey))
					//			{
					//				keyValuePairs.Add(orderKey, new List<ulong> { uniqueRecordOrdinal });
					//			}
					//			else
					//			{
					//				if (!keyValuePairs[orderKey].Contains(uniqueRecordOrdinal))
					//				{
					//					keyValuePairs[orderKey].Add(uniqueRecordOrdinal);  // Add the current record ordinal to the order
					//				}
					//			}
					//			uniqueRecCount++;
					//		}
					//	}
					//	break;
					//}
     //           }
     //           #endregion
     //       }
     //   }



        private void InitAllOrdersSchema()
		{
            foreach (var kvp in handleWithFieldDefs!.TableSchema!.Orders!)
            {
                if (kvp.Key != 0)  // Ignore the default order (0) which is the order of the table as it was created (i.e.; no order applied yet)
                {
                    var orderdef = handleWithFieldDefs.TableSchema!.Orders![kvp.Key];
                    Field f = fieldOrdinal2fieldDefinition![orderdef.FieldOrdinals[0]];
                    orders.Add(f.fieldDef.Name, new SortedList<string,List<ulong>>());
                }
            }
            currentOrder = 0;  // Set current order to 0 which is the default order (i.e.; no order applied yet)
        }


        private void ReadSetMember()
		{
            OrdinalResponse ordResponse = API.ReadTableSetMemberAsync(typedSetMember!, handleWithFieldDefs!).Result;
            recordOrdinal = ordResponse.Ordinal;  // This should be the same as the initial record ordinal
            lastRecordOrdinal = ordResponse.LastOrdinal;  // This should be the same as the last record ordinal
            isEoT = ordResponse.IsEoT;
            isBoT = ordResponse.IsBoT;
            isDeleted = ordResponse.IsDeleted;
            isDirty = ordResponse.IsDirty;  // should be false here
            for (int i = 0; i < handleWithFieldDefs!.TableSchema.FieldCount; i++)
			{
				fieldOrdinal2fieldDefinition![i] = new Field(handleWithFieldDefs!.TableSchema!.Fields![i], handleWithFieldDefs!.TableSchema!.Fields[i]!.GetObjectFromBytes(typedSetMember!.GetFieldBytes(i)));
            }
        }
		private void InitDefaultEvents()
		{
			NavigateOffDirtyRecord += TableNavigateOffDirtyRecord;
		}

		private void TableNavigateOffDirtyRecord( object sender, TableEventArgs e )
		{
			throw new NotImplementedException();
		}

		private void InitCache()
		{
			// Populate dictionary caches
			foreach (var kvp in handleWithFieldDefs!.TableSchema!.Fields!)
			{
				fieldName2fieldOrdinalMap!.Add( kvp.Value.Name.ToUpper(), kvp.Value.Ordinal );
				fieldOrdinal2fieldDefinition!.Add( kvp.Value.Ordinal, new Field( kvp.Value, new Uninitialized() ) );  // field value is unintialized (i.e. this is NOT the same as null)
			}
		}

        //private void InitCache(TypedSetMember typedSetMember)
        //{
        //    // Populate dictionary caches
        //    foreach (var kvp in handleWithFieldDefs!.TableSchema!.Fields!)
        //    {
        //        fieldName2fieldOrdinalMap!.Add(kvp.Value.Name.ToUpper(), kvp.Value.Ordinal);
        //        fieldOrdinal2fieldDefinition!.Add(kvp.Value.Ordinal, new Field(kvp.Value, kvp.Value.GetObjectFromBytes( typedSetMember.GetFieldBytes())));  // field value is unintialized (i.e. this is NOT the same as null)
        //    }
        //}

        private void ClearCache()
		{
			foreach(var kvp in handleWithFieldDefs!.TableSchema!.Fields! )
			{
				fieldOrdinal2fieldDefinition![kvp.Value.Ordinal] = new Field( kvp.Value, new Uninitialized() );
			}
		}
		#endregion

		#region Static Public Members
		#region Create Table
		static public Table Create(string absoluteTableName, Unosys.Common.Types.SetSchema tableSchema)
        {
			return new Table(absoluteTableName, tableSchema, (uint)FileMode.CreateNew, (uint)FileAccess.ReadWrite, (uint)FileAttributes.Normal, (uint)FileShare.None);
		}

		static public Table Create( string absoluteTableName, Unosys.Common.Types.SetSchema setSchema,  uint desiredAccess, uint setAttributes, uint shareMode )
		{
			return new Table( absoluteTableName, setSchema, (uint)FileMode.Create, desiredAccess, setAttributes, shareMode );
		}
		#endregion

		#region Open Table
		static public Table Open(string absoluteTableName)
		{
			return new Table(absoluteTableName, (uint)FileMode.Open, (uint)FileAccess.Read, (uint)FileAttributes.Normal, (uint)FileShare.None);
		}

		static public Table Open(string absoluteTableName, uint desiredAccess, uint setAttributes, uint shareMode)
		{
			return new Table(absoluteTableName, (uint)FileMode.Open, desiredAccess, setAttributes, shareMode);
		}
		#endregion
		#endregion

	}

    internal class Field
	{
		internal object Value = null!;
		internal FieldDef fieldDef = null!;

		internal Field( FieldDef fielddef, object value )
		{
			fieldDef = fielddef;
			Value = value;
		}
	}

	internal class Uninitialized {}

	public class TableEventArgs : EventArgs
	{
		public bool CancelNavigation = false;

		public TableEventArgs() : base() { }
	}
}

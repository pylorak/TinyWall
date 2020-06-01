
#include "TrafficMonitor.h"

#pragma comment(lib, "pdh.lib")
#include <pdh.h>
#include <pdhmsg.h>

struct TrafficMonitorState
{
	PDH_HQUERY hQuery = 0;
	PDH_HCOUNTER hTxCounter = 0;
	PDH_HCOUNTER hRxCounter = 0;
	char* buffer = 0;
	DWORD bufSize = 0;
};

static int64_t TrafficMonitor_ReadCounter(TrafficMonitorState *monitor, PDH_HCOUNTER hCounter)
{
	DWORD size = 0;
	DWORD count = 0;
	PdhGetFormattedCounterArray(hCounter, PDH_FMT_LARGE | PDH_FMT_NOSCALE | PDH_FMT_NOCAP100, &size, &count, NULL);

	if (size > monitor->bufSize)
	{
		if (monitor->buffer != NULL)
			delete [] monitor->buffer;
		monitor->buffer = new char[size];
		monitor->bufSize = size;
	}

	PDH_FMT_COUNTERVALUE_ITEM* items = reinterpret_cast<PDH_FMT_COUNTERVALUE_ITEM*>(monitor->buffer);
	PdhGetFormattedCounterArray(hCounter, PDH_FMT_LARGE | PDH_FMT_NOSCALE | PDH_FMT_NOCAP100, &size, &count, items);
	
	int64_t sum = 0;
	for (DWORD i = 0; i < count; ++i)
	{
		if ((items[i].FmtValue.CStatus == PDH_CSTATUS_VALID_DATA) || (items[i].FmtValue.CStatus == PDH_CSTATUS_NEW_DATA))
			sum += items[i].FmtValue.largeValue;
	}

	return sum;
}

DLLEXPORT TrafficMonitorState* DLLCALLCONV TrafficMonitor_Create()
{
	TrafficMonitorState* state = new TrafficMonitorState();

	PdhOpenQuery(NULL, NULL, &state->hQuery);
	PdhAddEnglishCounter(state->hQuery, L"\\Network Interface(*)\\Bytes Sent/Sec", NULL, &state->hTxCounter);
	PdhAddEnglishCounter(state->hQuery, L"\\Network Interface(*)\\Bytes Received/Sec", NULL, &state->hRxCounter);
	PdhCollectQueryData(state->hQuery);

	return state;
}

DLLEXPORT void DLLCALLCONV TrafficMonitor_Update(TrafficMonitorState *monitor, int64_t *txBytesPerSec, int64_t *rxBytesPerSec)
{
	PdhCollectQueryData(monitor->hQuery);
	*txBytesPerSec = TrafficMonitor_ReadCounter(monitor, monitor->hTxCounter);
	*rxBytesPerSec = TrafficMonitor_ReadCounter(monitor, monitor->hRxCounter);
}

DLLEXPORT void DLLCALLCONV TrafficMonitor_Delete(TrafficMonitorState *monitor)
{
	if (monitor->buffer != NULL)
		delete[] monitor->buffer;

	PdhCloseQuery(monitor->hQuery);
	delete monitor;
}

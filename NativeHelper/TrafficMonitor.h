#ifndef _TrafficMonitor_H_INCLUDED
#define _TrafficMonitor_H_INCLUDED

#include "common.h"
#include <cstdint>

struct TrafficMonitorState;

extern "C"
{
	DLLEXPORT TrafficMonitorState* DLLCALLCONV TrafficMonitor_Create();
	DLLEXPORT void DLLCALLCONV TrafficMonitor_Update(TrafficMonitorState* monitor, int64_t* txBytesPerSec, int64_t* rxBytesPerSec);
	DLLEXPORT void DLLCALLCONV TrafficMonitor_Delete(TrafficMonitorState* monitor);
}

#endif _TrafficMonitor_H_INCLUDED
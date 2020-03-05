#ifndef _UwpPackageListing_H_INCLUDED
#define _UwpPackageListing_H_INCLUDED

#define DLLEXPORT	__declspec(dllexport)
#define DLLCALLCONV	__stdcall

struct UwpPackage;

extern "C"
{
	DLLEXPORT void DLLCALLCONV GetUwpPackageListing(UwpPackage **arr, int *size);
}

#endif _UwpPackageListing_H_INCLUDED
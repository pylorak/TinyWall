#include "UwpPackageListing.h"

#pragma comment(lib, "windowsapp.lib")
#include <winrt/Windows.Management.Deployment.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.ApplicationModel.h>

#pragma comment(lib, "Ole32.lib")
#include <combaseapi.h>

#pragma comment(lib, "Userenv.lib")
#include <userenv.h>

#pragma comment(lib, "Advapi32.lib")
#include <sddl.h>

static wchar_t* CopyWinrtStr2CoTaskMem(const winrt::hstring &src) noexcept
{
	size_t numchars = (size_t)src.size() + 1;
	size_t bufsize = sizeof(wchar_t) * numchars;
	wchar_t* ret = (wchar_t*)CoTaskMemAlloc(bufsize);
	if (ret)
		lstrcpy(ret, src.c_str());
	return ret;
}

static wchar_t* CopyLpwstr2CoTaskMem(const LPWSTR src) noexcept
{
	size_t numchars = (size_t)lstrlen(src) + 1;
	size_t bufsize = sizeof(wchar_t) * numchars;
	wchar_t* ret = (wchar_t*)CoTaskMemAlloc(bufsize);
	if (ret)
		lstrcpy(ret, src);
	return ret;
}

struct UwpPackage
{
public:
	wchar_t* Name;
	wchar_t* Publisher;
	wchar_t* Sid;

	UwpPackage(const winrt::Windows::ApplicationModel::Package &package) noexcept :
		Name(CopyWinrtStr2CoTaskMem(package.Id().Name())),
		Publisher(CopyWinrtStr2CoTaskMem(package.Id().Publisher())),
		Sid(NULL)
	{
		PSID sid = NULL;
		LPWSTR strSid = NULL;

		if (S_OK != DeriveAppContainerSidFromAppContainerName(package.Id().FamilyName().c_str(), &sid))
			goto cleanup;

		if (!ConvertSidToStringSidW(sid, &strSid))
			goto cleanup;

		Sid = CopyLpwstr2CoTaskMem(strSid);

		cleanup:
		if (NULL != sid) FreeSid(sid);
		LocalFree(strSid);
	}
};

DLLEXPORT void DLLCALLCONV GetUwpPackageListing(UwpPackage **ret, int *size) 
{
	const int MAX_NUM_ELEM = 1024;

	UwpPackage *const arrBegin = (UwpPackage*)CoTaskMemAlloc(sizeof(UwpPackage) * MAX_NUM_ELEM);
	if (!arrBegin)
	{
		*ret = NULL;
		*size = 0;
	}

	winrt::Windows::Management::Deployment::PackageManager manager;
	winrt::Windows::Foundation::Collections::IIterable<winrt::Windows::ApplicationModel::Package> collection = manager.FindPackagesForUser(winrt::hstring());
	winrt::Windows::Foundation::Collections::IIterator<winrt::Windows::ApplicationModel::Package> packages = collection.First();

	static_assert(std::is_standard_layout<UwpPackage>::value);
	static_assert(std::is_trivially_copyable<UwpPackage>::value);

	int n = 0;
	UwpPackage *pPackage = arrBegin;

	do
	{
		new (pPackage) UwpPackage(packages.Current());
		++n; ++pPackage;
	} while (packages.MoveNext() && (n < MAX_NUM_ELEM));

	*ret = arrBegin;
	*size = n;
}

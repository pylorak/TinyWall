#include "UwpPackageListing.h"

#include <string>
#include <vector>

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

static wchar_t* CopyWinrtStr2CoTaskMem(const winrt::hstring &src)
{
	size_t numchars = (size_t)src.size() + 1;
	size_t bufsize = sizeof(wchar_t) * numchars;
	wchar_t* ret = (wchar_t*)CoTaskMemAlloc(bufsize);
	wcscpy_s(ret, numchars, src.c_str());
	return ret;
}

static wchar_t* CopyLpwstr2CoTaskMem(const LPWSTR src)
{
	size_t numchars = wcslen(src) + 1;
	size_t bufsize = sizeof(wchar_t) * numchars;
	wchar_t* ret = (wchar_t*)CoTaskMemAlloc(bufsize);
	wcscpy_s(ret, numchars, src);
	return ret;
}

struct UwpPackage
{
public:
	wchar_t* Name;
	wchar_t* Publisher;
	wchar_t* Sid;

	UwpPackage(const winrt::Windows::ApplicationModel::Package &package) :
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

DLLEXPORT void DLLCALLCONV GetUwpPackageListing(UwpPackage **arr, int *size)
{
	std::vector<UwpPackage> coll;
	winrt::Windows::Management::Deployment::PackageManager manager;
	winrt::Windows::Foundation::Collections::IIterable<winrt::Windows::ApplicationModel::Package> collection = manager.FindPackagesForUser(winrt::hstring());
	winrt::Windows::Foundation::Collections::IIterator<winrt::Windows::ApplicationModel::Package> packages = collection.First();

	static_assert(std::is_standard_layout<UwpPackage>::value);
	static_assert(std::is_trivially_copyable<UwpPackage>::value);

	do
	{
		coll.emplace_back(packages.Current());
	} while (packages.MoveNext());

	size_t n = coll.size();
	*size = static_cast<int>(n);
	*arr = (UwpPackage*)CoTaskMemAlloc(sizeof(UwpPackage) * n);
	memcpy(*arr, coll.data(), n * sizeof(UwpPackage));
}

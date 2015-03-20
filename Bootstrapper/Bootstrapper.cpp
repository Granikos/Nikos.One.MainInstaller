// Bootstrapper.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>
#include "Shlwapi.h"
#pragma comment(lib, "Shlwapi.lib")

bool isNetFrameworkInstalled()
{
	bool result = false;
	HKEY hKey;
	HKEY hKeyTmp;

	if (RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
	{
		if (
			// RegOpenKeyEx(hKey, L"v3.0", 0, KEY_READ, &hKeyTmp) == ERROR_SUCCESS ||
			// RegOpenKeyEx(hKey, L"v3.5", 0, KEY_READ, &hKeyTmp) == ERROR_SUCCESS ||
			RegOpenKeyEx(hKey, L"v4", 0, KEY_READ, &hKeyTmp) == ERROR_SUCCESS ||
			RegOpenKeyEx(hKey, L"v4.0", 0, KEY_READ, &hKeyTmp) == ERROR_SUCCESS ||
			RegOpenKeyEx(hKey, L"v4.5", 0, KEY_READ, &hKeyTmp) == ERROR_SUCCESS)
		{
			result = true;
			RegCloseKey(hKeyTmp);
		}

		RegCloseKey(hKey);
	}

	return result;
}

int _tmain(int argc, _TCHAR* argv[])
{
	TCHAR lpProcessDirectory[1024] = _T("");
	TCHAR lpCurrentDir[1024] = _T("");
	TCHAR lpPath[1024] = _T("");

	GetCurrentDirectory(
		sizeof(lpCurrentDir),
		lpCurrentDir
		);

	::PathCombine(lpProcessDirectory, lpCurrentDir, L"x64");

	PROCESS_INFORMATION processInformation;
	STARTUPINFO startupInfo;
	memset(&processInformation, 0, sizeof(processInformation));
	memset(&startupInfo, 0, sizeof(startupInfo));
	startupInfo.cb = sizeof(startupInfo);

	BOOL result;
	//TCHAR tempCmdLine[MAX_PATH * 2];  //Needed since CreateProcessW may change the contents of CmdLine

	if (!isNetFrameworkInstalled())
	{
		_tprintf(L"Before starting nikos one Installer, the Microsoft .NET Framework 4.5 must be installed.\r\nStrarting Microsoft .NET Framework 4.5 installation...");
		::PathCombine(lpPath, lpCurrentDir, L"x64\\Installers\\dotNetFx45_Full_setup.exe");
		result = ::CreateProcess(lpPath, NULL, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS, NULL, NULL, &startupInfo, &processInformation);

		WaitForSingleObject(processInformation.hProcess, INFINITE);
		CloseHandle(processInformation.hProcess);
		CloseHandle(processInformation.hThread);

		_tprintf(L"... installation of Microsoft .NET Framework 4.5 ended.");
	}

	::PathCombine(lpPath, lpCurrentDir, L"x64\\Installer.exe");
	result = ::CreateProcess(lpPath, NULL, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS, NULL, lpProcessDirectory, &startupInfo, &processInformation);

	DWORD error = ::GetLastError();
	if (error != 0)
	{
		TCHAR   lpBuffer[256] = _T("");
		::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM,                 // It´s a system error
			NULL,										// No string to be formatted needed
			error,										// Hey Windows: Please explain this error!
			MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),  // Do it in the standard language
			lpBuffer,									// Put the message here
			sizeof(lpBuffer),							// Number of bytes to store the message
			NULL);

		_tprintf(lpBuffer);

		return error;
	}

	return 0;
}


import os

BasePath = 'C:/Users/franc/Documents/SharpDevelop Projects/MapsInMyFolder/MapsInMyFolder/bin/Publication'
ListOfPath= ['/', '/fr/', '/x64/']


def getListOfFileInDirectory(Path):
    print("<?define MapsInMyFolderFiles" + Path.strip("/") + "=", end="")
    StringListOfFiles = "";
    for Filename in os.listdir(BasePath + Path):
        if os.path.isfile(BasePath + Path + Filename):
            if StringListOfFiles != "":
                StringListOfFiles = StringListOfFiles + ";"
            StringListOfFiles = StringListOfFiles + Filename
    
    print(StringListOfFiles + "?>")

for Path in ListOfPath:
    getListOfFileInDirectory(Path)
os.system("pause")
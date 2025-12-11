// QT Include(s)
#include <QStringList>
#include <QtPlugin>

class IPlugin {
public:
    virtual ~IPlugin() {}
    virtual int main(const QStringList& args) = 0;
};

QT_BEGIN_NAMESPACE

#define LAMPYRIS_PLUGIN_IID "com.lampyris.client.IPlugin"
Q_DECLARE_INTERFACE(IPlugin, LAMPYRIS_PLUGIN_IID)

QT_END_NAMESPACE

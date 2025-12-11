/****************************************************************************
** Meta object code from reading C++ file 'PluginEntryPoint.h'
**
** Created by: The Qt Meta Object Compiler version 68 (Qt 6.8.3)
**
** WARNING! All changes made in this file will be lost!
*****************************************************************************/

#include "../../../../../Lampyris.Client.Crypto/sources/PluginEntryPoint.h"
#include <QtCore/qmetatype.h>
#include <QtCore/qplugin.h>

#include <QtCore/qtmochelpers.h>

#include <memory>


#include <QtCore/qxptype_traits.h>
#if !defined(Q_MOC_OUTPUT_REVISION)
#error "The header file 'PluginEntryPoint.h' doesn't include <QObject>."
#elif Q_MOC_OUTPUT_REVISION != 68
#error "This file was generated using the moc from 6.8.3. It"
#error "cannot be used with the include files from this version of Qt."
#error "(The moc has changed too much.)"
#endif

#ifndef Q_CONSTINIT
#define Q_CONSTINIT
#endif

QT_WARNING_PUSH
QT_WARNING_DISABLE_DEPRECATED
QT_WARNING_DISABLE_GCC("-Wuseless-cast")
namespace {
struct qt_meta_tag_ZN16PluginEntryPointE_t {};
} // unnamed namespace


#ifdef QT_MOC_HAS_STRINGDATA
static constexpr auto qt_meta_stringdata_ZN16PluginEntryPointE = QtMocHelpers::stringData(
    "PluginEntryPoint"
);
#else  // !QT_MOC_HAS_STRINGDATA
#error "qtmochelpers.h not found or too old."
#endif // !QT_MOC_HAS_STRINGDATA

Q_CONSTINIT static const uint qt_meta_data_ZN16PluginEntryPointE[] = {

 // content:
      12,       // revision
       0,       // classname
       0,    0, // classinfo
       0,    0, // methods
       0,    0, // properties
       0,    0, // enums/sets
       0,    0, // constructors
       0,       // flags
       0,       // signalCount

       0        // eod
};

Q_CONSTINIT const QMetaObject PluginEntryPoint::staticMetaObject = { {
    QMetaObject::SuperData::link<QObject::staticMetaObject>(),
    qt_meta_stringdata_ZN16PluginEntryPointE.offsetsAndSizes,
    qt_meta_data_ZN16PluginEntryPointE,
    qt_static_metacall,
    nullptr,
    qt_incomplete_metaTypeArray<qt_meta_tag_ZN16PluginEntryPointE_t,
        // Q_OBJECT / Q_GADGET
        QtPrivate::TypeAndForceComplete<PluginEntryPoint, std::true_type>
    >,
    nullptr
} };

void PluginEntryPoint::qt_static_metacall(QObject *_o, QMetaObject::Call _c, int _id, void **_a)
{
    auto *_t = static_cast<PluginEntryPoint *>(_o);
    (void)_t;
    (void)_c;
    (void)_id;
    (void)_a;
}

const QMetaObject *PluginEntryPoint::metaObject() const
{
    return QObject::d_ptr->metaObject ? QObject::d_ptr->dynamicMetaObject() : &staticMetaObject;
}

void *PluginEntryPoint::qt_metacast(const char *_clname)
{
    if (!_clname) return nullptr;
    if (!strcmp(_clname, qt_meta_stringdata_ZN16PluginEntryPointE.stringdata0))
        return static_cast<void*>(this);
    if (!strcmp(_clname, "IPlugin"))
        return static_cast< IPlugin*>(this);
    if (!strcmp(_clname, "com.lampyris.client.IPlugin"))
        return static_cast< IPlugin*>(this);
    return QObject::qt_metacast(_clname);
}

int PluginEntryPoint::qt_metacall(QMetaObject::Call _c, int _id, void **_a)
{
    _id = QObject::qt_metacall(_c, _id, _a);
    return _id;
}

#ifdef QT_MOC_EXPORT_PLUGIN_V2
static constexpr unsigned char qt_pluginMetaDataV2_PluginEntryPoint[] = {
    0xbf, 
    // "IID"
    0x02,  0x78,  0x1b,  'c',  'o',  'm',  '.',  'l', 
    'a',  'm',  'p',  'y',  'r',  'i',  's',  '.', 
    'c',  'l',  'i',  'e',  'n',  't',  '.',  'I', 
    'P',  'l',  'u',  'g',  'i',  'n', 
    // "className"
    0x03,  0x70,  'P',  'l',  'u',  'g',  'i',  'n', 
    'E',  'n',  't',  'r',  'y',  'P',  'o',  'i', 
    'n',  't', 
    0xff, 
};
QT_MOC_EXPORT_PLUGIN_V2(PluginEntryPoint, PluginEntryPoint, qt_pluginMetaDataV2_PluginEntryPoint)
#else
QT_PLUGIN_METADATA_SECTION
Q_CONSTINIT static constexpr unsigned char qt_pluginMetaData_PluginEntryPoint[] = {
    'Q', 'T', 'M', 'E', 'T', 'A', 'D', 'A', 'T', 'A', ' ', '!',
    // metadata version, Qt version, architectural requirements
    0, QT_VERSION_MAJOR, QT_VERSION_MINOR, qPluginArchRequirements(),
    0xbf, 
    // "IID"
    0x02,  0x78,  0x1b,  'c',  'o',  'm',  '.',  'l', 
    'a',  'm',  'p',  'y',  'r',  'i',  's',  '.', 
    'c',  'l',  'i',  'e',  'n',  't',  '.',  'I', 
    'P',  'l',  'u',  'g',  'i',  'n', 
    // "className"
    0x03,  0x70,  'P',  'l',  'u',  'g',  'i',  'n', 
    'E',  'n',  't',  'r',  'y',  'P',  'o',  'i', 
    'n',  't', 
    0xff, 
};
QT_MOC_EXPORT_PLUGIN(PluginEntryPoint, PluginEntryPoint)
#endif  // QT_MOC_EXPORT_PLUGIN_V2

QT_WARNING_POP

/********************************************************************************
** Form generated from reading UI file 'LauncherWidget.ui'
**
** Created by: Qt User Interface Compiler version 6.8.3
**
** WARNING! All changes made in this file will be lost when recompiling UI file!
********************************************************************************/

#ifndef UI_LAUNCHERWIDGET_H
#define UI_LAUNCHERWIDGET_H

#include <QtCore/QVariant>
#include <QtWidgets/QApplication>
#include <QtWidgets/QLabel>
#include <QtWidgets/QProgressBar>
#include <QtWidgets/QSpacerItem>
#include <QtWidgets/QVBoxLayout>
#include <QtWidgets/QWidget>

QT_BEGIN_NAMESPACE

class Ui_LauncherWidget
{
public:
    QVBoxLayout *verticalLayout;
    QProgressBar *progressBar;
    QLabel *textStatus;
    QSpacerItem *__verticalSpacer;
    QLabel *textVersion;

    void setupUi(QWidget *LauncherWidget)
    {
        if (LauncherWidget->objectName().isEmpty())
            LauncherWidget->setObjectName("LauncherWidget");
        LauncherWidget->resize(400, 200);
        verticalLayout = new QVBoxLayout(LauncherWidget);
        verticalLayout->setObjectName("verticalLayout");
        progressBar = new QProgressBar(LauncherWidget);
        progressBar->setObjectName("progressBar");
        progressBar->setMinimum(0);
        progressBar->setMaximum(100);
        progressBar->setValue(0);

        verticalLayout->addWidget(progressBar);

        textStatus = new QLabel(LauncherWidget);
        textStatus->setObjectName("textStatus");

        verticalLayout->addWidget(textStatus);

        __verticalSpacer = new QSpacerItem(20, 40, QSizePolicy::Policy::Minimum, QSizePolicy::Policy::Expanding);

        verticalLayout->addItem(__verticalSpacer);

        textVersion = new QLabel(LauncherWidget);
        textVersion->setObjectName("textVersion");
        textVersion->setAlignment(Qt::AlignLeading);

        verticalLayout->addWidget(textVersion);


        retranslateUi(LauncherWidget);

        QMetaObject::connectSlotsByName(LauncherWidget);
    } // setupUi

    void retranslateUi(QWidget *LauncherWidget)
    {
        LauncherWidget->setWindowTitle(QCoreApplication::translate("LauncherWidget", "Startup Window", nullptr));
        textStatus->setText(QCoreApplication::translate("LauncherWidget", "Status: Initializing...", nullptr));
        textVersion->setText(QCoreApplication::translate("LauncherWidget", "Version: Unknown", nullptr));
    } // retranslateUi

};

namespace Ui {
    class LauncherWidget: public Ui_LauncherWidget {};
} // namespace Ui

QT_END_NAMESPACE

#endif // UI_LAUNCHERWIDGET_H
